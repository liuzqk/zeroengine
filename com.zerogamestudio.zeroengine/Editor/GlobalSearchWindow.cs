using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZeroEngine.AbilitySystem;
using ZeroEngine.Inventory; // Future proofing

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
#endif

namespace ZeroEngine.Editor
{
#if ODIN_INSPECTOR
    public class GlobalSearchWindow : OdinEditorWindow
    {
        [MenuItem("ZeroEngine/Tools/Global Search")]
        private static void OpenWindow()
        {
            var window = GetWindow<GlobalSearchWindow>();
            window.titleContent = new GUIContent("Global Search");
            window.Show();
        }

        [EnumToggleButtons]
        [BoxGroup("Search Filter", CenterLabel = true)]
        public SearchMode Mode = SearchMode.AbilityByComponent;

        [BoxGroup("Search Filter")]
        [ValueDropdown(nameof(GetAllComponentTypes))]
        [ShowIf(nameof(Mode), SearchMode.AbilityByComponent)]
        [LabelText("Component Type")]
        public string ComponentTypeSearch;

        [BoxGroup("Search Filter")]
        [HideIf(nameof(Mode), SearchMode.AbilityByComponent)]
        [LabelText("Search Name")]
        public string NameSearchTerm;

        [BoxGroup("Results")]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        public List<UnityEngine.Object> SearchResults = new List<UnityEngine.Object>();

        public enum SearchMode
        {
            AbilityByComponent,
            ItemByName,
            QuestByName
        }

        private IEnumerable<string> GetAllComponentTypes()
        {
            var baseType = typeof(ComponentData);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => baseType.IsAssignableFrom(p) && !p.IsAbstract)
                .Select(t => t.Name)
                .OrderBy(n => n);
        }

        [Button(ButtonSizes.Large), BoxGroup("Search Filter")]
        public void PerformSearch()
        {
            SearchResults.Clear();

            if (Mode == SearchMode.AbilityByComponent)
            {
                if (string.IsNullOrEmpty(ComponentTypeSearch)) return;
                SearchAbilitiesByComponent();
            }
            else if (Mode == SearchMode.ItemByName)
            {
                SearchAssets<InventoryItemSO>("t:InventoryItemSO", NameSearchTerm);
            }
            else if (Mode == SearchMode.QuestByName)
            {
                SearchAssets<ZeroEngine.Quest.QuestConfigSO>("t:QuestConfigSO", NameSearchTerm);
            }
        }

        private void SearchAssets<T>(string filter, string nameFilter) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    if (string.IsNullOrEmpty(nameFilter) || asset.name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SearchResults.Add(asset);
                    }
                }
            }
        }

        private void SearchAbilitiesByComponent()
        {
            string[] guids = AssetDatabase.FindAssets("t:AbilityDataSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ability = AssetDatabase.LoadAssetAtPath<AbilityDataSO>(path);
                
                if (ability != null && AbilityHasComponent(ability, ComponentTypeSearch))
                {
                    SearchResults.Add(ability);
                }
            }
        }

        private bool AbilityHasComponent(AbilityDataSO ability, string typeName)
        {
            // Check Triggers, Conditions, Effects
            if (CheckList(ability.Triggers, typeName)) return true;
            if (CheckList(ability.Conditions, typeName)) return true;
            if (CheckList(ability.Effects, typeName)) return true;
            return false;
        }

        private bool CheckList<T>(List<T> list, string typeName)
        {
            if (list == null) return false;
            foreach (var item in list)
            {
                if (item != null && item.GetType().Name.Contains(typeName)) return true;
            }
            return false;
        }
    }
#else
    public class GlobalSearchWindow : EditorWindow
    {
        [MenuItem("ZeroEngine/Tools/Global Search")]
        private static void OpenWindow() => GetWindow<GlobalSearchWindow>("Global Search");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Global Search requires Odin Inspector for Advanced Filtering.", MessageType.Warning);
        }
    }
#endif
}
