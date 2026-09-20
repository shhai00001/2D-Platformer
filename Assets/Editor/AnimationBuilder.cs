using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Platformer;

namespace Platformer.EditorTools
{
    /// <summary>
    /// 生成 AnimationClip 与 AnimatorController。
    ///
    /// 每个状态一段 Sprite 序列帧动画；攻击动画上挂 Animation Event，
    /// 由它在对的帧调用 AttackHitbox.PerformSwing()，这样判定和画面天然对齐。
    /// </summary>
    public static class AnimationBuilder
    {
        public const string AnimFolder = "Assets/Generated/Animations";

        /// <summary>SpriteRenderer 相对 Animator 根节点的路径，Prefab 里必须有个叫 Visual 的子物体。</summary>
        public const string VisualPath = "Visual";

        // ---------- 通用工具 ----------

        /// <summary>用一串 Sprite 生成逐帧动画。</summary>
        static AnimationClip MakeSpriteClip(string name, Sprite[] frames, float fps, bool loop)
        {
            string path = AnimFolder + "/" + name + ".anim";
            AssetDatabase.DeleteAsset(path);

            AnimationClip clip = new AnimationClip { name = name, frameRate = fps };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = VisualPath,
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        /// <summary>
        /// 追加一个 Animation Event。time 会被夹到动画长度之内 ——
        /// 落在末尾之外的事件 Unity 会直接丢掉，攻击判定就永远不会触发。
        /// </summary>
        static void AddEvent(AnimationClip clip, float time, string functionName)
        {
            float t = Mathf.Clamp(time, 0f, Mathf.Max(0f, clip.length - 0.005f));

            List<AnimationEvent> events = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(clip));
            events.Add(new AnimationEvent { time = t, functionName = functionName });
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
        }

        static float FrameTime(AnimationClip clip, int frameIndex, float fps)
        {
            return Mathf.Clamp(frameIndex / fps, 0f, Mathf.Max(0f, clip.length - 0.005f));
        }

        static AnimatorState AddState(AnimatorStateMachine sm, string name, Motion motion, float x, float y)
        {
            AnimatorState state = sm.AddState(name, new Vector3(x, y, 0f));
            state.motion = motion;
            return state;
        }

        static AnimatorStateTransition To(AnimatorState from, AnimatorState to,
                                          bool hasExitTime = false, float exitTime = 1f, float duration = 0f)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = hasExitTime;
            t.exitTime = exitTime;
            t.duration = duration;
            t.hasFixedDuration = true;
            return t;
        }

        static void Greater(AnimatorStateTransition t, string param, float value)
            => t.AddCondition(AnimatorConditionMode.Greater, value, param);

        static void Less(AnimatorStateTransition t, string param, float value)
            => t.AddCondition(AnimatorConditionMode.Less, value, param);

        static void IsFalse(AnimatorStateTransition t, string param)
            => t.AddCondition(AnimatorConditionMode.IfNot, 0f, param);

        static void IsTrue(AnimatorStateTransition t, string param)
            => t.AddCondition(AnimatorConditionMode.If, 0f, param);

        /// <summary>触发器条件。和 IsTrue 等价，只是名字让意图更清楚。</summary>
        static void Trigger(AnimatorStateTransition t, string param)
            => t.AddCondition(AnimatorConditionMode.If, 0f, param);

        // ---------- 玩家 Animator ----------

        public static AnimatorController BuildPlayerController(PixelArtGenerator.CharacterArt art)
        {
            PixelArtGenerator.EnsureFolder(AnimFolder);
            string path = AnimFolder + "/Player.controller";
            AssetDatabase.DeleteAsset(path);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            const float attackFps = 12f;

            AnimationClip idleClip   = MakeSpriteClip("Player_Idle",   art.idle,   4f,        true);
            AnimationClip moveClip   = MakeSpriteClip("Player_Run",    art.run,    12f,       true);
            AnimationClip jumpClip   = MakeSpriteClip("Player_Jump",   art.jump,   1f,        false);
            AnimationClip fallClip   = MakeSpriteClip("Player_Fall",   art.fall,   1f,        false);
            AnimationClip attackClip = MakeSpriteClip("Player_Attack", art.attack, attackFps, false);
            // 单帧受击动画用 4fps，正好凑出 0.25 秒的硬直时长
            AnimationClip hurtClip   = MakeSpriteClip("Player_Hurt",   art.hurt,   4f,        false);
            AnimationClip dieClip    = MakeSpriteClip("Player_Die",    art.death,  1f,        false);

            // 第 3 帧是剑身挥到身前的姿势 —— 判定就打在这一帧上
            AddEvent(attackClip, FrameTime(attackClip, 2, attackFps), "AnimationEvent_AttackHit");
            // 收招帧通知状态机可以退出攻击状态
            AddEvent(attackClip, attackClip.length - 0.01f, "AnimationEvent_AttackEnd");

            AnimatorState idle   = AddState(sm, "Idle",   idleClip,   0f,   0f);
            AnimatorState move   = AddState(sm, "Move",   moveClip,   260f, 0f);
            AnimatorState jump   = AddState(sm, "Jump",   jumpClip,   0f,   150f);
            AnimatorState fall   = AddState(sm, "Fall",   fallClip,   260f, 150f);
            AnimatorState attack = AddState(sm, "Attack", attackClip, 540f, 0f);
            AnimatorState hurt   = AddState(sm, "Hurt",   hurtClip,   540f, 150f);
            AnimatorState die    = AddState(sm, "Die",    dieClip,    540f, 300f);

            sm.defaultState = idle;

            // --- 地面移动 ---
            Greater(To(idle, move), "Speed", 0.1f);
            Less(To(move, idle), "Speed", 0.1f);

            // --- 起跳 / 下落 ---
            // 只靠垂直速度判断，比起判定"是否踩在地上"要快一帧，起跳更跟手
            Greater(To(idle, jump), "VerticalSpeed", 0.5f);
            Greater(To(move, jump), "VerticalSpeed", 0.5f);
            Less(To(idle, fall), "VerticalSpeed", -0.5f);
            Less(To(move, fall), "VerticalSpeed", -0.5f);

            // 到达最高点转为下落
            Less(To(jump, fall), "VerticalSpeed", 0.5f);
            // 二段跳（maxAirJumps 设为 1 时才会用到）
            Greater(To(fall, jump), "VerticalSpeed", 0.5f);

            // --- 落地 ---
            AnimatorStateTransition landIdle = To(fall, idle);
            IsTrue(landIdle, "Grounded"); Less(landIdle, "Speed", 0.1f);
            AnimatorStateTransition landMove = To(fall, move);
            IsTrue(landMove, "Grounded"); Greater(landMove, "Speed", 0.1f);

            // --- 出招：地面和空中都能触发 ---
            Trigger(To(idle, attack), "Attack");
            Trigger(To(move, attack), "Attack");
            Trigger(To(jump, attack), "Attack");
            Trigger(To(fall, attack), "Attack");

            // --- 收招：空中优先，其次按速度回 Move / Idle ---
            AnimatorStateTransition atkAir = To(attack, fall, true, 0.85f, 0.05f);
            IsFalse(atkAir, "Grounded");
            AnimatorStateTransition atkMove = To(attack, move, true, 0.85f, 0.05f);
            Greater(atkMove, "Speed", 0.1f);
            To(attack, idle, true, 0.85f, 0.05f);

            // --- 受击恢复 ---
            AnimatorStateTransition hurtAir = To(hurt, fall, true, 1f, 0f);
            IsFalse(hurtAir, "Grounded");
            AnimatorStateTransition hurtMove = To(hurt, move, true, 1f, 0f);
            Greater(hurtMove, "Speed", 0.1f);
            To(hurt, idle, true, 1f, 0f);

            // --- 任意状态都能被打断 ---
            // Die 放在前面，优先级高于 Hurt；canTransitionToSelf 关掉，避免原地反复触发
            AnimatorStateTransition toDie = sm.AddAnyStateTransition(die);
            toDie.hasExitTime = false; toDie.duration = 0f; toDie.canTransitionToSelf = false;
            Trigger(toDie, "Die");

            AnimatorStateTransition toHurt = sm.AddAnyStateTransition(hurt);
            toHurt.hasExitTime = false; toHurt.duration = 0f; toHurt.canTransitionToSelf = false;
            Trigger(toHurt, "Hurt");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        // ---------- 敌人 Animator ----------

        public static AnimatorController BuildEnemyController(PixelArtGenerator.CharacterArt art)
        {
            PixelArtGenerator.EnsureFolder(AnimFolder);
            string path = AnimFolder + "/Enemy.controller";
            AssetDatabase.DeleteAsset(path);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            const float walkFps = 8f;
            const float attackFps = 8f;

            AnimationClip idleClip   = MakeSpriteClip("Slime_Idle",   art.idle,   3f,        true);
            AnimationClip walkClip   = MakeSpriteClip("Slime_Walk",   art.run,    walkFps,   true);
            AnimationClip attackClip = MakeSpriteClip("Slime_Attack", art.attack, attackFps, false);
            AnimationClip hurtClip   = MakeSpriteClip("Slime_Hurt",   art.hurt,   4f,        false);
            AnimationClip dieClip    = MakeSpriteClip("Slime_Die",    art.death,  1f,        false);

            // 张嘴扑咬的那一帧
            AddEvent(attackClip, FrameTime(attackClip, 1, attackFps), "AnimationEvent_AttackHit");
            AddEvent(attackClip, attackClip.length - 0.01f, "AnimationEvent_AttackEnd");

            AnimatorState idle   = AddState(sm, "Idle",   idleClip,   0f,   0f);
            AnimatorState walk   = AddState(sm, "Walk",   walkClip,   260f, 0f);
            AnimatorState attack = AddState(sm, "Attack", attackClip, 520f, 0f);
            AnimatorState hurt   = AddState(sm, "Hurt",   hurtClip,   520f, 150f);
            AnimatorState die    = AddState(sm, "Die",    dieClip,    520f, 300f);

            sm.defaultState = idle;

            // 速度由 EnemyController 每帧写入，站着和走着的切换完全靠它
            Greater(To(idle, walk), "Speed", 0.15f);
            Less(To(walk, idle), "Speed", 0.15f);

            Trigger(To(idle, attack), "Attack");
            Trigger(To(walk, attack), "Attack");

            AnimatorStateTransition backToWalk = To(attack, walk, true, 0.85f, 0.05f);
            Greater(backToWalk, "Speed", 0.15f);
            To(attack, idle, true, 0.85f, 0.05f);

            AnimatorStateTransition hurtToWalk = To(hurt, walk, true, 1f, 0f);
            Greater(hurtToWalk, "Speed", 0.15f);
            To(hurt, idle, true, 1f, 0f);

            AnimatorStateTransition toDie = sm.AddAnyStateTransition(die);
            toDie.hasExitTime = false; toDie.duration = 0f; toDie.canTransitionToSelf = false;
            Trigger(toDie, "Die");

            AnimatorStateTransition toHurt = sm.AddAnyStateTransition(hurt);
            toHurt.hasExitTime = false; toHurt.duration = 0f; toHurt.canTransitionToSelf = false;
            Trigger(toHurt, "Hurt");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }
    }
}
