#if !ODIN_INSPECTOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZeroEngine.AbilitySystem;

namespace ZeroEngine.Combat.Editor
{
    /// <summary>
    /// AbilityDataSO 自定义编辑器 — 非 Odin 环境下提供友好的组件管理
    /// 有 Odin 时此 Editor 不加载，由 Odin 自动处理
    /// </summary>
    [CustomEditor(typeof(AbilityDataSO))]
    public class AbilityDataSOEditor : UnityEditor.Editor
    {
        private bool _triggersFoldout = true;
        private bool _conditionsFoldout = true;
        private bool _effectsFoldout = true;

        // 缓存的类型列表
        private static Type[] _triggerTypes;
        private static Type[] _conditionTypes;
        private static Type[] _effectTypes;

        private void OnEnable()
        {
            CacheComponentTypes();
        }

        private static void CacheComponentTypes()
        {
            if (_triggerTypes != null) return;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            _triggerTypes = assemblies
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .Where(t => !t.IsAbstract && typeof(TriggerComponentData).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();

            _conditionTypes = assemblies
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .Where(t => !t.IsAbstract && typeof(ConditionComponentData).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();

            _effectTypes = assemblies
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .Where(t => !t.IsAbstract && typeof(EffectComponentData).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ---- 基础信息 ----
            EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AbilityName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Icon"));

            EditorGUILayout.Space(8);

            // ---- 施放配置 ----
            EditorGUILayout.LabelField("施放配置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CastTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RecoveryTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Interruptible"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BaseCooldown"));

            EditorGUILayout.Space(8);

            // ---- 等级配置 ----
            EditorGUILayout.LabelField("等级配置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxLevel"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("EffectScalePerLevel"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CooldownReductionPerLevel"));

            EditorGUILayout.Space(12);

            // ---- TCE 组件 ----
            DrawComponentList("触发器 Triggers", serializedObject.FindProperty("Triggers"),
                ref _triggersFoldout, _triggerTypes, new Color(0.6f, 0.9f, 0.6f));

            DrawComponentList("条件 Conditions", serializedObject.FindProperty("Conditions"),
                ref _conditionsFoldout, _conditionTypes, new Color(0.9f, 0.9f, 0.6f));

            DrawComponentList("效果 Effects", serializedObject.FindProperty("Effects"),
                ref _effectsFoldout, _effectTypes, new Color(0.9f, 0.6f, 0.6f));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawComponentList(string label, SerializedProperty listProp,
            ref bool foldout, Type[] availableTypes, Color headerColor)
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = headerColor;
            EditorGUILayout.BeginVertical("helpBox");
            GUI.backgroundColor = prevBg;

            // 标题行
            EditorGUILayout.BeginHorizontal();

            foldout = EditorGUILayout.Foldout(foldout,
                $"{label} ({listProp.arraySize})", true, EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            // 添加按钮
            if (GUILayout.Button("+ 添加", EditorStyles.miniButtonRight, GUILayout.Width(60)))
            {
                ShowAddMenu(listProp, availableTypes);
            }

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                if (listProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("无组件。点击 [+ 添加] 按钮添加。", MessageType.Info);
                }

                int deleteIndex = -1;
                int moveUpIndex = -1;
                int moveDownIndex = -1;

                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var element = listProp.GetArrayElementAtIndex(i);
                    var instance = element.managedReferenceValue;
                    string typeName = instance?.GetType().Name ?? "(null)";
                    string displayName = FormatTypeName(typeName);

                    EditorGUILayout.BeginVertical("box");

                    // 元素标题行
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{i}] {displayName}", EditorStyles.boldLabel);

                    // 上移
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("▲", GUILayout.Width(22)))
                        moveUpIndex = i;
                    GUI.enabled = true;

                    // 下移
                    GUI.enabled = i < listProp.arraySize - 1;
                    if (GUILayout.Button("▼", GUILayout.Width(22)))
                        moveDownIndex = i;
                    GUI.enabled = true;

                    // 删除
                    var prevColor = GUI.color;
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                        deleteIndex = i;
                    GUI.color = prevColor;

                    EditorGUILayout.EndHorizontal();

                    // 元素属性
                    if (instance != null)
                    {
                        EditorGUI.indentLevel++;
                        DrawSerializedReferenceProperties(element);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                // 延迟操作避免迭代中修改
                if (deleteIndex >= 0)
                {
                    listProp.DeleteArrayElementAtIndex(deleteIndex);
                }
                else if (moveUpIndex > 0)
                {
                    listProp.MoveArrayElement(moveUpIndex, moveUpIndex - 1);
                }
                else if (moveDownIndex >= 0 && moveDownIndex < listProp.arraySize - 1)
                {
                    listProp.MoveArrayElement(moveDownIndex, moveDownIndex + 1);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void ShowAddMenu(SerializedProperty listProp, Type[] types)
        {
            var menu = new GenericMenu();

            foreach (var type in types)
            {
                var t = type;
                string displayName = FormatTypeName(t.Name);
                menu.AddItem(new GUIContent(displayName), false, () =>
                {
                    serializedObject.Update();
                    listProp.arraySize++;
                    var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                    newElement.managedReferenceValue = Activator.CreateInstance(t);
                    serializedObject.ApplyModifiedProperties();
                });
            }

            if (types.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("没有可用的组件类型"));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// 绘制 [SerializeReference] 对象的所有可序列化字段
        /// </summary>
        private void DrawSerializedReferenceProperties(SerializedProperty prop)
        {
            var endProp = prop.GetEndProperty();
            prop.NextVisible(true); // 进入子属性

            while (!SerializedProperty.EqualContents(prop, endProp))
            {
                EditorGUILayout.PropertyField(prop, true);
                if (!prop.NextVisible(false))
                    break;
            }
        }

        /// <summary>
        /// 格式化类型名：移除 Data/Component 后缀，加空格分词
        /// "DamageEffectData" → "Damage Effect"
        /// "OnHitTriggerData" → "On Hit Trigger"
        /// </summary>
        private static string FormatTypeName(string typeName)
        {
            var name = typeName
                .Replace("ComponentData", "")
                .Replace("Data", "");

            // PascalCase → 空格分词
            var sb = new System.Text.StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                    sb.Append(' ');
                sb.Append(name[i]);
            }
            return sb.ToString();
        }
    }
}
#endif
