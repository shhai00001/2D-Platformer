using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Platformer.EditorTools
{
    /// <summary>Tag / Layer 的安全读写。Tag 不存在时直接赋值会抛异常，这里统一兜住。</summary>
    public static class GameSetupUtil
    {
        public static bool TagExists(string tag)
        {
            string[] tags = InternalEditorUtility.tags;
            for (int i = 0; i < tags.Length; i++)
                if (tags[i] == tag) return true;
            return false;
        }

        public static bool LayerExists(string layerName)
        {
            return LayerMask.NameToLayer(layerName) >= 0;
        }

        /// <summary>Tag 不存在就跳过并给出提示，不抛异常。</summary>
        public static void SafeSetTag(GameObject go, string tag)
        {
            if (go == null) return;
            if (!TagExists(tag))
            {
                Debug.LogWarning($"[GameSetupUtil] Tag \"{tag}\" 不存在，已跳过设置。" +
                                 "请先执行 Tools/2D闯关游戏/② 初始化 Tag 与 Layer。");
                return;
            }
            go.tag = tag;
        }

        /// <summary>给物体及其所有子物体设置 Layer。</summary>
        public static void SetLayerRecursive(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
            }
        }

        // ---------- 私有 [SerializeField] 字段的读写 ----------
        // 生成器需要给私有字段接线，用 SerializedObject 比把字段改成 public 更干净。

        public static SerializedProperty Find(Object target, string field)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[GameSetupUtil] {target.GetType().Name} 上找不到序列化字段 " +
                                 $"\"{field}\"，字段名可能被改过。");
            }
            return p;
        }

        public static void SetRef(Object target, string field, Object value)
        {
            SerializedProperty p = Find(target, field);
            if (p == null) return;
            p.objectReferenceValue = value;
            p.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string field, int value)
        {
            SerializedProperty p = Find(target, field);
            if (p == null) return;
            p.intValue = value;
            p.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            SerializedProperty p = Find(target, field);
            if (p == null) return;
            p.floatValue = value;
            p.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string field, bool value)
        {
            SerializedProperty p = Find(target, field);
            if (p == null) return;
            p.boolValue = value;
            p.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetEnum(Object target, string field, int enumIndex)
        {
            SerializedProperty p = Find(target, field);
            if (p == null) return;
            p.enumValueIndex = enumIndex;
            p.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetVector2(Object target, string field, Vector2 value)
        {
            SerializedProperty p = Find(target, field);
            if (p == null) return;
            p.vector2Value = value;
            p.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>把若干个 Layer 名字合成掩码。</summary>
        public static int MaskOf(params string[] layerNames)
        {
            int mask = 0;
            for (int i = 0; i < layerNames.Length; i++)
            {
                int layer = LayerMask.NameToLayer(layerNames[i]);
                if (layer >= 0) mask |= 1 << layer;
            }
            return mask;
        }
    }
}
