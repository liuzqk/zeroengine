using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ZeroEngine.Editor
{
    /// <summary>
    /// 插件检测器 - 通过反射检测常用第三方插件状态
    /// 无硬依赖，可在任何项目中使用
    /// </summary>
    [InitializeOnLoad]
    public static class PluginManager
    {
        public static bool HasOdin { get; private set; }
        public static bool HasDOTween { get; private set; }
        public static bool HasEasySave { get; private set; }
        public static bool HasYooAsset { get; private set; }

        static PluginManager()
        {
            CheckPlugins();
        }

        public static void CheckPlugins()
        {
            // 通过反射检测类型，无硬依赖
            HasOdin = TypeExists("Sirenix.OdinInspector.Editor.OdinMenuEditorWindow");
            HasDOTween = TypeExists("DG.Tweening.DOTween");
            HasEasySave = TypeExists("ES3");
            HasYooAsset = TypeExists("YooAsset.YooAssets");
        }

        private static bool TypeExists(string typeName)
        {
            var type = System.Type.GetType(typeName);
            if (type != null) return true;

            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null) return true;
            }
            return false;
        }

        public static void DrawPluginStatusGUI()
        {
            EditorGUILayout.LabelField("Plugin Integration Status", EditorStyles.boldLabel);

            DrawStatus("Odin Inspector", HasOdin, "Enables Advanced Editors (Inventory, etc.)");
            DrawStatus("DOTween", HasDOTween, "Enables UI Animations");
            DrawStatus("EasySave 3", HasEasySave, "Enables Robust Save System");
            DrawStatus("YooAsset", HasYooAsset, "Enables Industrial Asset Management");

            EditorGUILayout.Space();
        }

        private static void DrawStatus(string name, bool present, string benefit)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = present ? Color.green : Color.yellow;
            GUILayout.Label(present ? "✔" : "✘", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label(name, GUILayout.Width(120));
            GUILayout.Label(benefit, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
}
