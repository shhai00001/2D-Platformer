using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Platformer;

namespace Platformer.EditorTools
{
    /// <summary>
    /// 项目级配置：Tag、Layer、Build Settings。
    /// Tag 和 Layer 都改 ProjectSettings/TagManager.asset，
    /// 只能用 SerializedObject 操作，没有现成的 API。
    /// </summary>
    public static class ProjectSetup
    {
        /// <summary>代码里会用到、但不依赖它的 Tag（判定走组件而非字符串匹配）。</summary>
        static readonly string[] RequiredTags = { "Player", "Enemy", "Goal" };

        /// <summary>地面/玩家/敌人三层的名字，攻击判定和地面检测的 LayerMask 都靠它们。</summary>
        static readonly string[] RequiredLayers =
        {
            GameLayers.Ground,
            GameLayers.Player,
            GameLayers.Enemy,
        };

        static SerializedObject GetTagManager()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[ProjectSetup] 读不到 ProjectSettings/TagManager.asset，" +
                               "请在 Unity 里正常打开本项目后重试。");
                return null;
            }
            return new SerializedObject(assets[0]);
        }

        public static void EnsureTags()
        {
            SerializedObject so = GetTagManager();
            if (so == null) return;

            SerializedProperty tagsProp = so.FindProperty("tags");
            if (tagsProp == null) return;

            for (int i = 0; i < RequiredTags.Length; i++)
            {
                string tag = RequiredTags[i];

                bool exists = false;
                for (int j = 0; j < tagsProp.arraySize; j++)
                {
                    if (tagsProp.GetArrayElementAtIndex(j).stringValue == tag) { exists = true; break; }
                }
                if (exists) continue;

                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                Debug.Log($"[ProjectSetup] 新增 Tag：{tag}");
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        public static void EnsureLayers()
        {
            SerializedObject so = GetTagManager();
            if (so == null) return;

            SerializedProperty layersProp = so.FindProperty("layers");
            if (layersProp == null) return;

            for (int i = 0; i < RequiredLayers.Length; i++)
            {
                string layerName = RequiredLayers[i];
                if (LayerMask.NameToLayer(layerName) >= 0) continue;

                // 0..7 是 Unity 内置层，用户层从索引 8 开始
                int slot = -1;
                for (int j = 8; j < layersProp.arraySize; j++)
                {
                    if (string.IsNullOrEmpty(layersProp.GetArrayElementAtIndex(j).stringValue))
                    {
                        slot = j;
                        break;
                    }
                }

                if (slot < 0)
                {
                    Debug.LogError($"[ProjectSetup] 没有空闲的用户 Layer 槽位，无法创建 \"{layerName}\"。" +
                                   "请在 Project Settings > Tags and Layers 里手动腾一个位置。");
                    continue;
                }

                layersProp.GetArrayElementAtIndex(slot).stringValue = layerName;
                Debug.Log($"[ProjectSetup] 在 Layer {slot} 新增：{layerName}");
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        /// <summary>把生成的场景按顺序写进 Build Settings，SceneManager.LoadScene 才能用场景名加载。</summary>
        public static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            for (int i = 0; i < SceneNames.All.Length; i++)
            {
                string path = SceneBuilder.SceneFolder + "/" + SceneNames.All[i] + ".unity";
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[ProjectSetup] 场景文件还不存在，已跳过：{path}");
                    continue;
                }
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[ProjectSetup] Build Settings 已写入 {scenes.Count} 个场景（第一个是启动场景）");
        }
    }
}
