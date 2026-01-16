using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ZeroEngine.Tutorial;

namespace ZeroEngine.Editor.Tutorial
{
    public class TutorialEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab;
        private readonly string[] _tabNames = { "教程列表", "步骤编辑", "统计" };

        private List<TutorialSO> _tutorials = new List<TutorialSO>();
        private List<TutorialStepSO> _steps = new List<TutorialStepSO>();
        private TutorialSO _selectedTutorial;
        private string _searchFilter = "";

        [MenuItem("ZeroEngine/Tutorial/Tutorial Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<TutorialEditorWindow>("Tutorial Editor");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            RefreshAssets();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawTutorialList(); break;
                case 1: DrawStepEditor(); break;
                case 2: DrawStatistics(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                RefreshAssets();

            GUILayout.FlexibleSpace();

            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));

            if (GUILayout.Button("+教程", EditorStyles.toolbarButton, GUILayout.Width(50)))
                CreateNew<TutorialSO>("New Tutorial");

            if (GUILayout.Button("+步骤", EditorStyles.toolbarButton, GUILayout.Width(50)))
                CreateNew<TutorialStepSO>("New Step");

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTutorialList()
        {
            foreach (var tutorial in _tutorials)
            {
                if (tutorial == null) continue;
                if (!MatchesFilter(tutorial.DisplayName, tutorial.TutorialId)) continue;

                bool isSelected = _selectedTutorial == tutorial;
                var style = isSelected ? "selectionRect" : "box";

                EditorGUILayout.BeginHorizontal(style);

                // 图标
                if (tutorial.Icon != null)
                    GUILayout.Label(AssetPreview.GetAssetPreview(tutorial.Icon), GUILayout.Width(40), GUILayout.Height(40));
                else
                    GUILayout.Label(EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow"), GUILayout.Width(40), GUILayout.Height(40));

                // 信息
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(tutorial.DisplayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{tutorial.StepCount} 步骤 | 优先级: {tutorial.Priority}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // 标签
                if (tutorial.OneTime) GUILayout.Label("一次性", EditorStyles.miniLabel);
                if (!tutorial.Skippable) GUILayout.Label("不可跳过", EditorStyles.miniLabel);

                if (GUILayout.Button("选择", GUILayout.Width(50)))
                {
                    _selectedTutorial = tutorial;
                    _selectedTab = 1;
                }

                if (GUILayout.Button("编辑", GUILayout.Width(50)))
                    Selection.activeObject = tutorial;

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawStepEditor()
        {
            if (_selectedTutorial == null)
            {
                EditorGUILayout.HelpBox("请先选择一个教程", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"教程: {_selectedTutorial.DisplayName}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            for (int i = 0; i < _selectedTutorial.Steps.Count; i++)
            {
                var step = _selectedTutorial.Steps[i];
                if (step == null) continue;

                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(25));

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(step.Title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"触发: {step.TriggerType} | 高亮: {step.Highlights.Count}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                if (!step.Skippable) GUILayout.Label("🔒", GUILayout.Width(20));
                if (step.PauseGame) GUILayout.Label("⏸", GUILayout.Width(20));

                if (GUILayout.Button("↑", GUILayout.Width(25)) && i > 0)
                {
                    Swap(_selectedTutorial.Steps, i, i - 1);
                    EditorUtility.SetDirty(_selectedTutorial);
                }

                if (GUILayout.Button("↓", GUILayout.Width(25)) && i < _selectedTutorial.Steps.Count - 1)
                {
                    Swap(_selectedTutorial.Steps, i, i + 1);
                    EditorUtility.SetDirty(_selectedTutorial);
                }

                if (GUILayout.Button("编辑", GUILayout.Width(50)))
                    Selection.activeObject = step;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("+ 添加步骤"))
            {
                string path = EditorUtility.SaveFilePanelInProject("创建步骤", "New Step", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var step = CreateInstance<TutorialStepSO>();
                    AssetDatabase.CreateAsset(step, path);
                    _selectedTutorial.Steps.Add(step);
                    EditorUtility.SetDirty(_selectedTutorial);
                    AssetDatabase.SaveAssets();
                    RefreshAssets();
                }
            }
        }

        private void DrawStatistics()
        {
            EditorGUILayout.LabelField("教程统计", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField($"教程总数: {_tutorials.Count}");
            EditorGUILayout.LabelField($"步骤总数: {_steps.Count}");

            int totalSteps = 0;
            foreach (var t in _tutorials)
                if (t != null) totalSteps += t.StepCount;
            EditorGUILayout.LabelField($"教程内步骤总数: {totalSteps}");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("按优先级统计", EditorStyles.boldLabel);

            var priorityCounts = new Dictionary<TutorialPriority, int>();
            foreach (var t in _tutorials)
            {
                if (t == null) continue;
                if (!priorityCounts.ContainsKey(t.Priority))
                    priorityCounts[t.Priority] = 0;
                priorityCounts[t.Priority]++;
            }

            foreach (var kvp in priorityCounts)
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
        }

        private void RefreshAssets()
        {
            _tutorials.Clear();
            _steps.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:TutorialSO"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TutorialSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) _tutorials.Add(asset);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:TutorialStepSO"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TutorialStepSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) _steps.Add(asset);
            }
        }

        private void CreateNew<T>(string defaultName) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject("创建", defaultName, "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                var asset = CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = asset;
                RefreshAssets();
            }
        }

        private bool MatchesFilter(params string[] values)
        {
            if (string.IsNullOrEmpty(_searchFilter)) return true;
            string filter = _searchFilter.ToLower();
            foreach (var v in values)
                if (!string.IsNullOrEmpty(v) && v.ToLower().Contains(filter))
                    return true;
            return false;
        }

        private void Swap<T>(List<T> list, int i, int j)
        {
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}