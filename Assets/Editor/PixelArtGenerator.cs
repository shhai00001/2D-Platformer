using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Platformer.EditorTools
{
    /// <summary>
    /// 程序化生成占位像素美术，让项目开箱即可运行。
    /// 全部输出到 Assets/Generated/Art，随时可以换成自己的素材 ——
    /// 只要把 Sprite 拖到对应 Prefab 的 SpriteRenderer 上即可，代码无需改动。
    /// </summary>
    public static class PixelArtGenerator
    {
        public const string ArtFolder = "Assets/Generated/Art";
        public const int PPU = 16;

        // ---------- 调色板 ----------
        static readonly Color32 Clear    = new Color32(0, 0, 0, 0);
        static readonly Color32 Outline  = new Color32(24, 22, 38, 255);
        static readonly Color32 Skin     = new Color32(243, 205, 165, 255);
        static readonly Color32 SkinDark = new Color32(206, 158, 118, 255);
        static readonly Color32 Hair     = new Color32(78, 56, 40, 255);
        static readonly Color32 HairLit  = new Color32(110, 80, 56, 255);
        static readonly Color32 Shirt    = new Color32(62, 128, 214, 255);
        static readonly Color32 ShirtLit = new Color32(96, 162, 240, 255);
        static readonly Color32 Pants    = new Color32(46, 62, 84, 255);
        static readonly Color32 Boot     = new Color32(38, 34, 48, 255);
        static readonly Color32 Blade    = new Color32(226, 232, 242, 255);
        static readonly Color32 BladeLit = new Color32(255, 255, 255, 255);
        static readonly Color32 Hilt     = new Color32(158, 104, 52, 255);

        static readonly Color32 Slime    = new Color32(122, 200, 108, 255);
        static readonly Color32 SlimeLit = new Color32(168, 232, 148, 255);
        static readonly Color32 SlimeDrk = new Color32(72, 142, 74, 255);
        static readonly Color32 Eye      = new Color32(30, 30, 40, 255);
        static readonly Color32 Tooth    = new Color32(245, 245, 235, 255);

        static readonly Color32 Grass    = new Color32(104, 186, 92, 255);
        static readonly Color32 GrassLit = new Color32(140, 214, 118, 255);
        static readonly Color32 Dirt     = new Color32(126, 92, 62, 255);
        static readonly Color32 DirtDark = new Color32(96, 68, 46, 255);
        static readonly Color32 DirtLit  = new Color32(150, 112, 78, 255);

        static readonly Color32 FlagRed  = new Color32(226, 84, 84, 255);
        static readonly Color32 Pole     = new Color32(196, 196, 204, 255);
        static readonly Color32 Gold     = new Color32(248, 208, 88, 255);

        // ---------- 像素画布 ----------

        /// <summary>一块可寻址的像素缓冲。坐标原点在左下角，跟 Unity 的纹理坐标一致。</summary>
        public class Canvas
        {
            public readonly int W, H;
            readonly Color32[] _px;

            public Canvas(int w, int h)
            {
                W = w;
                H = h;
                _px = new Color32[w * h];
                for (int i = 0; i < _px.Length; i++) _px[i] = Clear;
            }

            public void Set(int x, int y, Color32 c)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                _px[y * W + x] = c;
            }

            public Color32 Get(int x, int y)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return Clear;
                return _px[y * W + x];
            }

            public void Rect(int x, int y, int w, int h, Color32 c)
            {
                for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    Set(x + i, y + j, c);
            }

            public void Ellipse(int cx, int cy, int rx, int ry, Color32 c)
            {
                for (int j = -ry; j <= ry; j++)
                for (int i = -rx; i <= rx; i++)
                {
                    float nx = rx == 0 ? 0f : (float)i / rx;
                    float ny = ry == 0 ? 0f : (float)j / ry;
                    if (nx * nx + ny * ny <= 1.0f) Set(cx + i, cy + j, c);
                }
            }

            /// <summary>两点之间的粗线段，用来画剑和旗杆。</summary>
            public void Line(int x0, int y0, int x1, int y1, Color32 c, int thickness = 1)
            {
                int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                if (steps == 0) { Set(x0, y0, c); return; }
                for (int s = 0; s <= steps; s++)
                {
                    float t = (float)s / steps;
                    int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                    int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                    if (thickness <= 1) Set(x, y, c);
                    else Rect(x, y, thickness, thickness, c);
                }
            }

            /// <summary>
            /// 给整个剪影描一圈黑边：所有"自己是透明、但四邻里有不透明"的像素都涂成描边色。
            /// 这样不用手工画轮廓，形状改了描边也自动跟着变。
            /// </summary>
            public void ApplyOutline(Color32 outline)
            {
                Color32[] copy = (Color32[])_px.Clone();
                for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a != 0) continue;
                    bool touch = Solid(copy, x - 1, y) || Solid(copy, x + 1, y)
                              || Solid(copy, x, y - 1) || Solid(copy, x, y + 1);
                    if (touch) _px[y * W + x] = outline;
                }
            }

            /// <summary>该位置是否已经有画上东西（用于描边时判断邻居）。</summary>
            bool Solid(Color32[] px, int x, int y)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return false;
                return px[y * W + x].a != 0;
            }

            public Color32[] ToPixels() => _px;
        }

        // ---------- 资源输出 ----------

        /// <summary>把像素画布写成 PNG 并配置成像素风 Sprite。</summary>
        static Sprite WriteSprite(Canvas canvas, string fileName, Vector2 pivot, bool tileable = false)
        {
            EnsureFolder(ArtFolder);

            Texture2D tex = new Texture2D(canvas.W, canvas.H, TextureFormat.RGBA32, false);
            tex.SetPixels32(canvas.ToPixels());
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = tileable ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            tex.Apply();

            string path = ArtFolder + "/" + fileName + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PPU;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = tileable ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                // 平铺渲染要求 Sprite 是 FullRect 网格，否则 SpriteRenderer 的 Tiled 模式不生效
                settings.spriteMeshType = tileable ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                settings.spriteExtrude = 0;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void EnsureFolder(string folder)
        {
            // Unity 的资源路径一律用正斜杠；Path 系列在 Windows 上会返回反斜杠，统一转一下
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder);
            parent = string.IsNullOrEmpty(parent) ? string.Empty : parent.Replace('\\', '/');

            // 已经在 Assets 根上了，没有更上层可以递归
            if (string.IsNullOrEmpty(parent) || parent == folder) return;

            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        // ---------- 玩家 ----------

        const int PlayerW = 20;
        const int PlayerH = 26;

        /// <summary>
        /// 画一帧玩家。十几个动作帧共用这一段代码，只是参数不同，
        /// 所以后续微调动作只要改这里。
        /// 坐标系：画布 20x26，角色左右对称轴在 x = 10，脚底在 y = 1（留 1 像素描边）。
        /// </summary>
        static Canvas DrawPlayer(int legPhase, int bob, int armSwing, int swordStage, bool dead = false)
        {
            Canvas c = new Canvas(PlayerW, PlayerH);

            const int groundY = 1;

            // legPhase: 0 并拢 / 1 左腿抬 / 2 分开 / 3 右腿抬
            int leftLift = legPhase == 1 ? 1 : 0;
            int rightLift = legPhase == 3 ? 1 : 0;

            // 靴子与裤腿
            c.Rect(6, groundY + leftLift, 4, 2, Boot);
            c.Rect(10, groundY + rightLift, 4, 2, Boot);
            c.Rect(7, groundY + 2 + leftLift, 3, 5, Pants);
            c.Rect(10, groundY + 2 + rightLift, 3, 5, Pants);

            int torsoY = 8 + bob;

            // 躯干
            c.Rect(7, torsoY, 6, 9, Shirt);
            c.Rect(7, torsoY + 7, 6, 2, ShirtLit);
            c.Rect(7, torsoY, 6, 1, Pants);

            // 手臂（右手随 armSwing 上下摆）
            c.Rect(5, torsoY + 3, 2, 6, Shirt);
            c.Rect(13, torsoY + 3 + armSwing, 2, 6, Shirt);
            c.Rect(5, torsoY + 2, 2, 1, Skin);
            c.Rect(13, torsoY + 2 + armSwing, 2, 1, Skin);

            // 头部
            int headY = 16 + bob;
            c.Rect(6, headY, 8, 8, Skin);
            c.Rect(6, headY + 5, 8, 3, Hair);
            c.Rect(6, headY + 4, 1, 2, Hair);
            c.Rect(13, headY + 4, 1, 2, Hair);
            c.Rect(7, headY + 7, 6, 1, HairLit);
            c.Rect(7, headY + 2, 1, 1, SkinDark);
            c.Rect(12, headY + 2, 1, 1, SkinDark);

            if (dead)
            {
                c.Rect(8, headY + 3, 1, 1, Eye);
                c.Rect(11, headY + 3, 1, 1, Eye);
                c.Set(9, headY + 4, Eye);
                c.Set(10, headY + 4, Eye);
            }
            else
            {
                c.Rect(8, headY + 3, 1, 2, Eye);
                c.Rect(11, headY + 3, 1, 2, Eye);
                c.Set(8, headY + 4, BladeLit);
                c.Set(11, headY + 4, BladeLit);
            }

            DrawSword(c, 15, torsoY + 4 + armSwing, swordStage);

            c.ApplyOutline(Outline);
            return c;
        }

        /// <summary>0 = 收剑，1 = 抬手蓄力，2 = 挥出，3 = 收势。</summary>
        static void DrawSword(Canvas c, int handX, int handY, int stage)
        {
            switch (stage)
            {
                case 1:
                    c.Line(handX, handY, handX + 4, handY + 8, Blade, 2);
                    c.Line(handX, handY, handX + 4, handY + 8, BladeLit, 1);
                    c.Rect(handX - 2, handY - 1, 3, 2, Hilt);
                    break;
                case 2:
                    c.Rect(handX, handY, 5, 2, Blade);
                    c.Rect(handX, handY + 1, 5, 1, BladeLit);
                    c.Rect(handX - 2, handY - 1, 3, 3, Hilt);
                    break;
                case 3:
                    c.Line(handX, handY, handX + 4, handY - 7, Blade, 2);
                    c.Line(handX, handY, handX + 4, handY - 7, BladeLit, 1);
                    c.Rect(handX - 2, handY - 1, 3, 2, Hilt);
                    break;
            }
        }

        /// <summary>一个角色的全部动作帧。</summary>
        public class CharacterArt
        {
            public Sprite[] idle, run, jump, fall, attack, hurt, death;
        }

        public static CharacterArt BuildPlayerArt()
        {
            CharacterArt art = new CharacterArt();
            // 角色脚底画在 y = 1 那一行，pivot 也定在 1/26，两者对齐后角色才正好站在 transform 上
            Vector2 pivot = new Vector2(0.5f, 1f / PlayerH);

            art.idle = new[]
            {
                WriteSprite(DrawPlayer(0, 0, 0, 0), "player_idle_0", pivot),
                WriteSprite(DrawPlayer(0, 1, 0, 0), "player_idle_1", pivot),
            };
            art.run = new[]
            {
                WriteSprite(DrawPlayer(1, 0,  1, 0), "player_run_0", pivot),
                WriteSprite(DrawPlayer(2, 1,  0, 0), "player_run_1", pivot),
                WriteSprite(DrawPlayer(3, 0, -1, 0), "player_run_2", pivot),
                WriteSprite(DrawPlayer(2, 1,  0, 0), "player_run_3", pivot),
            };
            art.jump = new[] { WriteSprite(DrawPlayer(3, 1, -1, 0), "player_jump_0", pivot) };
            art.fall = new[] { WriteSprite(DrawPlayer(2, 0,  1, 0), "player_fall_0", pivot) };
            art.attack = new[]
            {
                WriteSprite(DrawPlayer(0, 0, 0, 1), "player_atk_0", pivot),
                WriteSprite(DrawPlayer(0, 1, 0, 1), "player_atk_1", pivot),
                WriteSprite(DrawPlayer(0, 0, 0, 2), "player_atk_2", pivot),
                WriteSprite(DrawPlayer(0, 1, 0, 2), "player_atk_3", pivot),
                WriteSprite(DrawPlayer(0, 0, 0, 3), "player_atk_4", pivot),
            };
            art.hurt = new[] { WriteSprite(DrawPlayer(2, 0, -1, 0), "player_hurt_0", pivot) };
            art.death = new[] { WriteSprite(DrawPlayer(2, 0, -1, 0, true), "player_death_0", pivot) };
            return art;
        }

        // ---------- 敌人（史莱姆） ----------

        const int SlimeW = 18;
        const int SlimeH = 18;

        /// <summary>
        /// 画一帧史莱姆。squash 控制被压扁的程度（做呼吸和跳跃挤压），
        /// 轮廓用一条抛物线算出，比手摆像素更圆润。
        /// </summary>
        static Canvas DrawSlime(int squash, int eyeShift, int mouthStage, bool dead = false)
        {
            Canvas c = new Canvas(SlimeW, SlimeH);

            const int groundY = 1;
            const int centerX = 9;
            const int halfWidth = 7;

            int height = Mathf.Max(5, 12 - squash);

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / Mathf.Max(1, height - 1);          // 0 = 底，1 = 顶
                float w = halfWidth * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t * 0.8f));
                int hw = Mathf.Max(2, Mathf.RoundToInt(w));
                c.Rect(centerX - hw, groundY + y, hw * 2, 1, Slime);
            }

            // 底部一圈深色，做出体积感
            c.Rect(centerX - halfWidth + 1, groundY, halfWidth * 2 - 2, 1, SlimeDrk);
            // 左上方高光
            c.Ellipse(centerX - 4, groundY + Mathf.RoundToInt(height * 0.62f), 2, 2, SlimeLit);

            int eyeY = groundY + Mathf.RoundToInt(height * 0.55f);
            int mouthY = groundY + Mathf.RoundToInt(height * 0.24f);

            if (dead)
            {
                // × 形眼睛
                c.Set(centerX - 3, eyeY, Eye); c.Set(centerX - 1, eyeY, Eye);
                c.Set(centerX - 2, eyeY + 1, Eye);
                c.Set(centerX - 3, eyeY + 2, Eye); c.Set(centerX - 1, eyeY + 2, Eye);
                c.Set(centerX + 1, eyeY, Eye); c.Set(centerX + 3, eyeY, Eye);
                c.Set(centerX + 2, eyeY + 1, Eye);
                c.Set(centerX + 1, eyeY + 2, Eye); c.Set(centerX + 3, eyeY + 2, Eye);
                c.Rect(centerX - 2, mouthY, 5, 1, Eye);
            }
            else
            {
                c.Rect(centerX - 3 + eyeShift, eyeY, 2, 3, Eye);
                c.Rect(centerX + 1 + eyeShift, eyeY, 2, 3, Eye);
                c.Set(centerX - 3 + eyeShift, eyeY + 2, BladeLit);
                c.Set(centerX + 1 + eyeShift, eyeY + 2, BladeLit);

                if (mouthStage == 0)
                {
                    c.Rect(centerX - 2, mouthY, 4, 1, Eye);
                }
                else
                {
                    // 张嘴露出两颗牙
                    c.Rect(centerX - 3, mouthY - 1, 6, 3, Eye);
                    c.Rect(centerX - 2, mouthY + 1, 1, 1, Tooth);
                    c.Rect(centerX + 1, mouthY + 1, 1, 1, Tooth);
                }
            }

            c.ApplyOutline(Outline);
            return c;
        }

        public static CharacterArt BuildEnemyArt()
        {
            CharacterArt art = new CharacterArt();
            Vector2 pivot = new Vector2(0.5f, 1f / SlimeH);

            art.idle = new[]
            {
                WriteSprite(DrawSlime(0, 0, 0), "slime_idle_0", pivot),
                WriteSprite(DrawSlime(1, 0, 0), "slime_idle_1", pivot),
            };
            art.run = new[]
            {
                WriteSprite(DrawSlime(2, 0, 0), "slime_walk_0", pivot),
                WriteSprite(DrawSlime(0, 1, 0), "slime_walk_1", pivot),
                WriteSprite(DrawSlime(2, 0, 0), "slime_walk_2", pivot),
                WriteSprite(DrawSlime(0, -1, 0), "slime_walk_3", pivot),
            };
            art.attack = new[]
            {
                WriteSprite(DrawSlime(3, 0, 0), "slime_atk_0", pivot),
                WriteSprite(DrawSlime(0, 0, 1), "slime_atk_1", pivot),
                WriteSprite(DrawSlime(2, 0, 1), "slime_atk_2", pivot),
            };
            art.hurt = new[] { WriteSprite(DrawSlime(3, 0, 0), "slime_hurt_0", pivot) };
            art.death = new[] { WriteSprite(DrawSlime(6, 0, 0, true), "slime_death_0", pivot) };
            art.jump = art.idle;
            art.fall = art.idle;
            return art;
        }

        // ---------- 场景与地形 ----------

        /// <summary>地面表层：上方几行草，下方泥土。</summary>
        static Canvas DrawGroundTile()
        {
            Canvas c = new Canvas(16, 16);
            c.Rect(0, 0, 16, 16, Dirt);
            c.Rect(0, 12, 16, 4, Grass);
            c.Rect(0, 15, 16, 1, GrassLit);
            // 草与土的交界做出参差感
            c.Set(2, 11, Grass); c.Set(5, 11, Grass); c.Set(9, 11, Grass); c.Set(13, 11, Grass);
            AddDirtSpecks(c, 16, 16, 12);
            return c;
        }

        /// <summary>纯泥土，用于地面块的下半部分。</summary>
        static Canvas DrawDirtTile()
        {
            Canvas c = new Canvas(16, 16);
            c.Rect(0, 0, 16, 16, Dirt);
            c.Rect(0, 15, 16, 1, DirtDark);
            AddDirtSpecks(c, 16, 16, 16);
            return c;
        }

        /// <summary>悬空平台：比地面薄一半（8 像素 = 0.5 世界单位）。</summary>
        static Canvas DrawPlatformTile()
        {
            Canvas c = new Canvas(16, 8);
            c.Rect(0, 0, 16, 8, Dirt);
            c.Rect(0, 5, 16, 3, Grass);
            c.Rect(0, 7, 16, 1, GrassLit);
            c.Set(0, 0, DirtDark); c.Set(15, 0, DirtDark);
            c.Rect(0, 0, 16, 1, DirtDark);
            return c;
        }

        /// <summary>撒泥土颗粒。刻意避开最外一圈，保证平铺时接缝不可见。</summary>
        static void AddDirtSpecks(Canvas c, int w, int h, int topLimit)
        {
            int[,] spots =
            {
                { 3, 8 }, { 4, 8 }, { 10, 6 }, { 11, 6 }, { 2, 4 },
                { 12, 10 }, { 6, 3 }, { 13, 4 }, { 7, 9 }, { 1, 6 }, { 14, 8 }
            };
            for (int i = 0; i < spots.GetLength(0); i++)
            {
                int x = spots[i, 0], y = spots[i, 1];
                if (x <= 0 || x >= w - 1 || y <= 0 || y >= topLimit) continue;
                c.Set(x, y, i % 2 == 0 ? DirtDark : DirtLit);
            }
        }

        /// <summary>终点旗子：旗杆 + 三角旗 + 底座。</summary>
        static Canvas DrawGoal()
        {
            Canvas c = new Canvas(16, 32);

            // 底座
            c.Rect(4, 0, 8, 2, DirtDark);
            c.Rect(5, 2, 6, 1, Pole);
            // 旗杆
            c.Rect(7, 3, 2, 27, Pole);
            c.Rect(7, 3, 1, 27, BladeLit);
            // 三角旗
            for (int y = 0; y < 11; y++)
            {
                int w = 7 - Mathf.Abs(y - 5);
                if (w > 0) c.Rect(9, 18 + y, w, 1, y % 3 == 0 ? Gold : FlagRed);
            }
            // 杆顶圆球
            c.Rect(6, 30, 4, 2, Gold);

            c.ApplyOutline(Outline);
            return c;
        }

        /// <summary>
        /// 远山剪影。只叠加整数频率的正弦，因此左右边缘天然衔接，
        /// 配合 Tiled 模式可以无缝平铺满整个关卡。
        /// </summary>
        static Canvas DrawHills(int w, int h, Color32 color, float seed)
        {
            Canvas c = new Canvas(w, h);
            for (int x = 0; x < w; x++)
            {
                float t = (float)x / w;
                float twoPi = Mathf.PI * 2f;
                float n = 0.42f
                        + 0.30f * Mathf.Sin(t * twoPi * 2f + seed)
                        + 0.15f * Mathf.Sin(t * twoPi * 5f + seed * 1.7f)
                        + 0.07f * Mathf.Sin(t * twoPi * 11f + seed * 2.3f);
                int top = Mathf.Clamp(Mathf.RoundToInt(h * n), 2, h);
                c.Rect(x, 0, 1, top, color);
            }
            return c;
        }

        /// <summary>天空渐变，会被拉伸铺满整个背景。</summary>
        static Canvas DrawSky()
        {
            Canvas c = new Canvas(8, 64);
            Color32 bottom = new Color32(158, 208, 236, 255);
            Color32 top    = new Color32(58, 96, 168, 255);
            for (int y = 0; y < 64; y++)
            {
                float t = (float)y / 63f;
                // 平方一下，让靠近地平线的亮色区域更大
                Color32 col = Lerp32(bottom, top, t * t);
                c.Rect(0, y, 8, 1, col);
            }
            return c;
        }

        static Color32 Lerp32(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(a.r + (b.r - a.r) * t),
                (byte)Mathf.RoundToInt(a.g + (b.g - a.g) * t),
                (byte)Mathf.RoundToInt(a.b + (b.b - a.b) * t),
                255);
        }

        /// <summary>一次生成全部占位美术。</summary>
        public class GeneratedArt
        {
            public CharacterArt player;
            public CharacterArt enemy;
            public Sprite groundTile;
            public Sprite dirtTile;
            public Sprite platformTile;
            public Sprite goal;
            public Sprite sky;
            public Sprite hillsFar;
            public Sprite hillsNear;
        }

        public static GeneratedArt GenerateAll()
        {
            EnsureFolder(ArtFolder);

            GeneratedArt art = new GeneratedArt
            {
                player = BuildPlayerArt(),
                enemy = BuildEnemyArt(),
                // 地形用 Tiled 模式渲染。pivot 取中心，摆放时按"地块中心"定位最不容易算错
                groundTile   = WriteSprite(DrawGroundTile(),   "tile_ground",   new Vector2(0.5f, 0.5f), true),
                dirtTile     = WriteSprite(DrawDirtTile(),     "tile_dirt",     new Vector2(0.5f, 0.5f), true),
                platformTile = WriteSprite(DrawPlatformTile(), "tile_platform", new Vector2(0.5f, 0.5f), true),
                goal         = WriteSprite(DrawGoal(),         "goal_flag",     new Vector2(0.5f, 0f)),
                sky          = WriteSprite(DrawSky(),          "bg_sky",        new Vector2(0.5f, 0.5f)),
                hillsFar     = WriteSprite(DrawHills(128, 56, new Color32(104, 140, 176, 255), 3.1f),
                                           "bg_hills_far",  new Vector2(0.5f, 0f), true),
                hillsNear    = WriteSprite(DrawHills(128, 48, new Color32(66, 102, 140, 255), 7.7f),
                                           "bg_hills_near", new Vector2(0.5f, 0f), true),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return art;
        }
    }
}
