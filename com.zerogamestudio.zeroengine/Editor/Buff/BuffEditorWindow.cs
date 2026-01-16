#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using System.Linq;
using ZeroEngine.BuffSystem;

namespace ZeroEngine.Editor.Buff
{
    public class BuffEditorWindow : OdinMenuEditorWindow
    {
        [MenuItem("ZeroEngine/Buff Editor")]
        private static void OpenWindow()
        {
            var window = GetWindow<BuffEditorWindow>();
            window.Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Config.DrawSearchToolbar = true;
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;

            tree.Add("Create New Buff", new CreateNewBuffData());
            tree.AddAllAssetsAtPath("Buffs", "Assets/Data/Buffs", typeof(BuffData), true, true);
            
            // Add Icons
            tree.EnumerateTree().Where(x => x.Value is BuffData).ForEach(node =>
            {
                var buff = node.Value as BuffData;
                if (buff != null && buff.Icon != null)
                {
                    node.Icon = buff.Icon.texture;
                }
                else
                {
                    node.Icon = EditorIcons.StarPointer.Active; // Default icon
                }
            });

            tree.SortMenuItemsByName();
            return tree;
        }

        public class CreateNewBuffData
        {
            [LabelText("Buff Name")]
            public string BuffName = "NewBuff";

            [Button("Create Buff")]
            public void Create()
            {
                string path = "Assets/Data/Buffs";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                    AssetDatabase.Refresh();
                }

                var asset = ScriptableObject.CreateInstance<BuffData>();
                asset.BuffId = System.Guid.NewGuid().ToString(); // Auto-generate ID
                
                string fullPath = $"{path}/{BuffName}.asset";
                fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
                
                AssetDatabase.CreateAsset(asset, fullPath);
                AssetDatabase.SaveAssets();

                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }
        }
    }
}
#endif
