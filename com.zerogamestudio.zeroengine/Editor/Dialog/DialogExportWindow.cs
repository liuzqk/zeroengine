using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ZeroEngine.Editor.Dialog
{
    /// <summary>
    /// Editor tool to export dialogue text to CSV for localization.
    /// </summary>
    public class DialogExportWindow : EditorWindow
    {
        private List<ZeroEngine.Dialog.DialogueSO> _dialogues = new List<ZeroEngine.Dialog.DialogueSO>();
        private string _exportPath = "Assets/Localization/Dialog_Export.csv";
        private Vector2 _scrollPos;

        [MenuItem("ZeroEngine/Dialog/Export to CSV")]
        public static void ShowWindow()
        {
            GetWindow<DialogExportWindow>("Dialog CSV Export");
        }

        private void OnGUI()
        {
            GUILayout.Label("Dialog CSV Export Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Export path
            EditorGUILayout.BeginHorizontal();
            _exportPath = EditorGUILayout.TextField("Export Path", _exportPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.SaveFilePanel("Save CSV", "Assets", "Dialog_Export", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    _exportPath = path.StartsWith(Application.dataPath) 
                        ? "Assets" + path.Substring(Application.dataPath.Length) 
                        : path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Find all DialogueSO assets
            if (GUILayout.Button("Find All DialogueSO Assets"))
            {
                FindAllDialogues();
            }

            EditorGUILayout.LabelField($"Found: {_dialogues.Count} DialogueSO assets");

            // List of dialogues
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
            foreach (var d in _dialogues)
            {
                EditorGUILayout.ObjectField(d, typeof(ZeroEngine.Dialog.DialogueSO), false);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Export button
            GUI.enabled = _dialogues.Count > 0;
            if (GUILayout.Button("Export to CSV", GUILayout.Height(40)))
            {
                ExportToCSV();
            }
            GUI.enabled = true;
        }

        private void FindAllDialogues()
        {
            _dialogues.Clear();
            string[] guids = AssetDatabase.FindAssets("t:DialogueSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ZeroEngine.Dialog.DialogueSO>(path);
                if (asset != null)
                    _dialogues.Add(asset);
            }
        }

        private void ExportToCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Asset,EntryIndex,Speaker,Text,LocalizationKey,VoiceKey,ChoiceIndex,ChoiceText,ChoiceLocKey");

            foreach (var dialogue in _dialogues)
            {
                string assetName = dialogue.name;
                for (int i = 0; i < dialogue.Entries.Count; i++)
                {
                    var entry = dialogue.Entries[i];
                    
                    // Main entry line
                    sb.AppendLine($"\"{assetName}\",{i},\"{Escape(entry.Speaker)}\",\"{Escape(entry.Text)}\",\"{entry.LocalizationKey}\",\"{entry.VoiceKey}\",,,");
                    
                    // Choice lines
                    if (entry.Choices != null)
                    {
                        for (int c = 0; c < entry.Choices.Count; c++)
                        {
                            var choice = entry.Choices[c];
                            sb.AppendLine($"\"{assetName}\",{i},,,,,{c},\"{Escape(choice.Text)}\",\"{choice.LocalizationKey}\"");
                        }
                    }
                }
            }

            File.WriteAllText(_exportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[DialogExport] Exported {_dialogues.Count} dialogues to: {_exportPath}");
            EditorUtility.DisplayDialog("Export Complete", $"Exported to:\n{_exportPath}", "OK");
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\"", "\"\"").Replace("\n", "\\n").Replace("\r", "");
        }
    }
}
