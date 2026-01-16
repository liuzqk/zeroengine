using UnityEditor;
using UnityEngine;
using ZeroEngine.Core;
using ZeroEngine.Save;

namespace ZeroEngine.Editor
{
    public class ZeroEngineDashboard : EditorWindow
    {
        [MenuItem("ZeroEngine/Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<ZeroEngineDashboard>("Zero Engine");
        }

        private void OnGUI()
        {
            GUILayout.Label("Zero Engine Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            PluginManager.DrawPluginStatusGUI();
            EditorGUILayout.Space();

            DrawNetworkModuleSection();
            EditorGUILayout.Space();

            DrawSpineSection();
            EditorGUILayout.Space();

            DrawEditorTools();
            EditorGUILayout.Space();

            DrawQuickTools();
            EditorGUILayout.Space();
            DrawDocumentation();
        }

        private void DrawEditorTools()
        {
            GUILayout.Label("Editor Tools", EditorStyles.boldLabel);

            // Visual Graph Editors row
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dialog Graph Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Dialog/Dialog Graph Editor");
            }
            if (GUILayout.Button("BT Graph Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/BehaviorTree/BT Graph Editor");
            }
            if (GUILayout.Button("Talent Tree Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/TalentTree/Talent Tree Editor");
            }
            EditorGUILayout.EndHorizontal();

            // Runtime Viewers row
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("BehaviorTree Viewer", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/BehaviorTree Viewer");
            }
            EditorGUILayout.EndHorizontal();

            // Data Editors row
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Buff Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Buff Editor");
            }
            if (GUILayout.Button("Ability Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Tools/Ability Editor");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Inventory Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Inventory Editor");
            }
            if (GUILayout.Button("Equipment Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Equipment/Equipment Editor");
            }
            if (GUILayout.Button("Quest Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Quest Editor");
            }
            EditorGUILayout.EndHorizontal();

            // New Systems row (v1.11.0)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Loot Table Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Loot/Loot Table Editor");
            }
            if (GUILayout.Button("Achievement Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Achievement/Achievement Editor");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Crafting Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Crafting/Recipe Editor");
            }
            if (GUILayout.Button("Relationship Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Relationship/Relationship Editor");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Global Search", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Tools/Global Search %g");
            }
            if (GUILayout.Button("Translation Checker", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Localization/Translation Checker");
            }
            EditorGUILayout.EndHorizontal();

            // New Systems row (v1.12.0)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Shop Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Shop/Shop Editor");
            }
            if (GUILayout.Button("Tutorial Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Tutorial/Tutorial Editor");
            }
            if (GUILayout.Button("Calendar Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Calendar/Calendar Editor");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Notification Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Notification/Notification Editor");
            }
            if (GUILayout.Button("Settings Editor", GUILayout.Height(28)))
            {
                EditorApplication.ExecuteMenuItem("ZeroEngine/Settings/Settings Editor");
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawNetworkModuleSection()
        {
            GUILayout.Label("Optional Modules", EditorStyles.boldLabel);
            
            // Network / Multiplayer
            EditorGUILayout.BeginHorizontal();
#if ZEROENGINE_NETCODE
            GUI.color = Color.green;
            GUILayout.Label("✔", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label("Netcode for GameObjects", GUILayout.Width(180));
            GUILayout.Label("Multiplayer networking", EditorStyles.miniLabel);
#else
            GUI.color = Color.yellow;
            GUILayout.Label("✘", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label("Netcode for GameObjects", GUILayout.Width(180));
            if (GUILayout.Button("Install", GUILayout.Width(60)))
            {
                InstallNetworkPackages();
            }
#endif
            EditorGUILayout.EndHorizontal();
            
            // Spine
            EditorGUILayout.BeginHorizontal();
#if SPINE_UNITY
            GUI.color = Color.green;
            GUILayout.Label("✔", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label("Spine Runtime", GUILayout.Width(180));
            GUILayout.Label("SpineSkin module active", EditorStyles.miniLabel);
#else
            GUI.color = Color.yellow;
            GUILayout.Label("✘", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label("Spine Runtime", GUILayout.Width(180));
            if (GUILayout.Button("Download", GUILayout.Width(60)))
            {
                Application.OpenURL("https://esotericsoftware.com/spine-unity-download");
            }
#endif
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSpineSection()
        {
            // Moved to DrawNetworkModuleSection for unified display
        }
        
        private void InstallNetworkPackages()
        {
            // Add NGO and related packages to manifest
            string manifestPath = "Packages/manifest.json";
            string manifest = System.IO.File.ReadAllText(manifestPath);
            
            // Check if already has NGO
            if (manifest.Contains("com.unity.netcode.gameobjects"))
            {
                Debug.Log("[ZeroEngine] Netcode for GameObjects is already in manifest.");
                return;
            }
            
            // Insert NGO package
            string insertPoint = "\"dependencies\": {";
            string packagesToAdd = @"""dependencies"": {
    ""com.unity.netcode.gameobjects"": ""1.8.0"",
    ""com.unity.multiplayer.tools"": ""2.2.0"",
    ""com.unity.services.relay"": ""1.1.1"",
    ""com.unity.services.lobby"": ""1.2.2"",";
            
            manifest = manifest.Replace(insertPoint, packagesToAdd);
            System.IO.File.WriteAllText(manifestPath, manifest);
            
            Debug.Log("[ZeroEngine] Added Multiplayer packages to manifest. Unity will now import them.");
            UnityEditor.PackageManager.Client.Resolve();
        }

        private ZeroEngine.Inventory.InventoryItemSO _debugItem;
        private int _debugAmount = 1;

        private void DrawQuickTools()
        {
            GUILayout.Label("Quick Tools", EditorStyles.label);
            
            if (GUILayout.Button("Open Persistent Data Path"))
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            }

            if (GUILayout.Button("Clear PlayerPrefs"))
            {
                PlayerPrefs.DeleteAll();
                Debug.Log("PlayerPrefs Cleared.");
            }
            
            if (GUILayout.Button("Clear Save Data (EasySave)"))
            {
                 // We can't access runtime Managers unless playing, but we can delete file
                 var path = System.IO.Path.Combine(Application.persistentDataPath, SaveManager.DefaultSaveFile);
                 if (System.IO.File.Exists(path))
                 {
                     System.IO.File.Delete(path);
                     Debug.Log($"Deleted {path}");
                 }
                 else
                 {
                     Debug.Log("No Save File found.");
                 }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Inventory Debug (Runtime Only)", EditorStyles.label);
            
            _debugItem = (ZeroEngine.Inventory.InventoryItemSO)EditorGUILayout.ObjectField("Item", _debugItem, typeof(ZeroEngine.Inventory.InventoryItemSO), false);
            _debugAmount = EditorGUILayout.IntField("Amount", _debugAmount);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Add Item"))
            {
                if (ZeroEngine.Inventory.InventoryManager.Instance != null && _debugItem != null)
                {
                    ZeroEngine.Inventory.InventoryManager.Instance.AddItem(_debugItem, _debugAmount);
                    Debug.Log($"[Dashboard] Added {_debugAmount} x {_debugItem.name}");
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDocumentation()
        {
            GUILayout.Label("Documentation", EditorStyles.label);
            if (GUILayout.Button("Open Documentation"))
            {
                // Find and open the local documentation file
                string[] guids = AssetDatabase.FindAssets("Documentation t:TextAsset", new[] { "Assets/ZeroEngine" });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    AssetDatabase.OpenAsset(asset);
                }
                else
                {
                    // Fallback: Open folder
                    EditorUtility.RevealInFinder("Assets/ZeroEngine/Documentation.md");
                }
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Setup YooAsset Rules (Auto)"))
            {
                YooAssetSetup.SetupDefaultRules();
            }
        }
    }
}
