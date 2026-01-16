using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ZeroEngine.TalentTree;

namespace ZeroEngine.Editor.TalentTree
{
    /// <summary>
    /// 天赋节点属性面板
    /// </summary>
    public class TalentNodeInspector : VisualElement
    {
        private TalentNodeSO _currentNode;
        private TalentTreeSO _currentTree;
        private ScrollView _scrollView;
        private VisualElement _contentContainer;

        public TalentNodeInspector()
        {
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 8;
            style.paddingBottom = 8;

            // 标题
            var titleLabel = new Label("Node Inspector");
            titleLabel.style.fontSize = 14;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8;
            Add(titleLabel);

            // 滚动容器
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1;
            Add(_scrollView);

            _contentContainer = new VisualElement();
            _scrollView.Add(_contentContainer);

            ShowEmpty();
        }

        public void ClearSelection()
        {
            _currentNode = null;
            _currentTree = null;
            ShowEmpty();
        }

        public void ShowNode(TalentNodeSO node, TalentTreeSO tree)
        {
            _currentNode = node;
            _currentTree = tree;

            if (node == null)
            {
                ShowEmpty();
                return;
            }

            _contentContainer.Clear();

            // 使用 SerializedObject 绑定
            var serializedNode = new SerializedObject(node);

            // 基础信息
            AddSection("Basic Info");
            AddPropertyField(serializedNode, "NodeId");
            AddPropertyField(serializedNode, "DisplayName");
            AddPropertyField(serializedNode, "Description");
            AddPropertyField(serializedNode, "Icon");
            AddPropertyField(serializedNode, "NodeType");

            // 等级配置
            AddSection("Level");
            AddPropertyField(serializedNode, "MaxLevel");
            AddPropertyField(serializedNode, "PointCostPerLevel");

            // 前置条件
            AddSection("Prerequisites");
            AddPropertyField(serializedNode, "Prerequisites");
            AddPropertyField(serializedNode, "PrerequisiteMinLevel");
            AddPropertyField(serializedNode, "RequiredCharacterLevel");

            // 效果
            AddSection("Effects");
            AddPropertyField(serializedNode, "Effects");

            // 添加效果按钮
            var addEffectButton = new Button(() => ShowAddEffectMenu()) { text = "+ Add Effect" };
            addEffectButton.style.marginTop = 4;
            _contentContainer.Add(addEffectButton);

            // 删除节点按钮
            AddSection("Actions");
            var deleteButton = new Button(() => DeleteNode()) { text = "Delete Node" };
            deleteButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
            deleteButton.style.marginTop = 8;
            _contentContainer.Add(deleteButton);

            // 应用更改
            serializedNode.ApplyModifiedProperties();
        }

        private void ShowEmpty()
        {
            _contentContainer.Clear();
            var label = new Label("Select a node to edit");
            label.style.color = new Color(0.6f, 0.6f, 0.6f);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = 20;
            _contentContainer.Add(label);
        }

        private void AddSection(string title)
        {
            var label = new Label(title);
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 12;
            label.style.marginBottom = 4;
            label.style.color = new Color(0.8f, 0.8f, 0.8f);
            _contentContainer.Add(label);

            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            separator.style.marginBottom = 4;
            _contentContainer.Add(separator);
        }

        private void AddPropertyField(SerializedObject serializedObject, string propertyPath)
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property != null)
            {
                var field = new PropertyField(property);
                field.Bind(serializedObject);
                field.RegisterValueChangeCallback(evt =>
                {
                    EditorUtility.SetDirty(_currentNode);
                });
                _contentContainer.Add(field);
            }
        }

        private void ShowAddEffectMenu()
        {
            if (_currentNode == null) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Stat Modifier"), false, () => AddEffect(new StatModifierEffect()));
            menu.AddItem(new GUIContent("Multi Stat Modifier"), false, () => AddEffect(new MultiStatModifierEffect()));
            menu.AddItem(new GUIContent("Buff Effect"), false, () => AddEffect(new BuffEffect()));
            menu.AddItem(new GUIContent("Unlock Ability"), false, () => AddEffect(new UnlockAbilityEffect()));
            menu.AddItem(new GUIContent("Custom Effect"), false, () => AddEffect(new CustomEffect()));
            menu.ShowAsContext();
        }

        private void AddEffect(TalentEffect effect)
        {
            if (_currentNode == null) return;

            _currentNode.Effects.Add(effect);
            EditorUtility.SetDirty(_currentNode);

            // 刷新显示
            ShowNode(_currentNode, _currentTree);
        }

        private void DeleteNode()
        {
            if (_currentNode == null || _currentTree == null) return;

            if (!EditorUtility.DisplayDialog("Delete Node",
                $"Are you sure you want to delete '{_currentNode.DisplayName}'?",
                "Delete", "Cancel"))
            {
                return;
            }

            // 移除连接
            _currentTree.Connections.RemoveAll(c =>
                c.FromNodeId == _currentNode.NodeId ||
                c.ToNodeId == _currentNode.NodeId);

            // 移除节点
            _currentTree.Nodes.Remove(_currentNode);

            // 删除资产
            AssetDatabase.RemoveObjectFromAsset(_currentNode);
            Object.DestroyImmediate(_currentNode, true);

            EditorUtility.SetDirty(_currentTree);
            AssetDatabase.SaveAssets();

            // 清空面板
            ClearSelection();

            // 刷新编辑器
            var window = EditorWindow.GetWindow<TalentTreeEditorWindow>();
            window?.RefreshView();
        }
    }
}
