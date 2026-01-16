# ZeroEngine Editor Tools

> **Version**: v1.5.0+
> **Last Updated**: 2026-01-01

---

## Overview

ZeroEngine provides a suite of editor tools for visual editing and debugging of game systems.

## Quick Access

All tools are accessible from:
- **Menu**: ZeroEngine > [Tool Name]
- **Dashboard**: ZeroEngine > Dashboard > Editor Tools section

---

## Available Tools

### 1. Dialog Graph Editor (v1.5.0+)

**Menu**: ZeroEngine > Dialog > Dialog Graph Editor

Node-based visual editor for DialogGraphSO assets:
- Create and edit dialog graphs visually
- 8 node types with color-coded visualization
- Drag-and-drop node connections
- Right-click context menu for adding nodes
- Side panel for editing node properties
- Graph structure validation

**Node Types**:
| Type | Color | Description |
|------|-------|-------------|
| Start | Green | Entry point of dialog |
| End | Red | Exit point with optional end tag |
| Text | Blue | Display text with speaker/portrait |
| Choice | Orange | Player choice options |
| Condition | Purple | Branch based on expression |
| SetVariable | Yellow | Modify dialog variables |
| Random | Cyan | Random branch with weights |
| Callback | Pink | External callback trigger |

**Keyboard Shortcuts**:
| Shortcut | Action |
|----------|--------|
| Ctrl+D | Duplicate selected node |
| Delete | Delete selected node |
| Right-click | Open add node menu |

**Usage**:
1. Create a DialogGraphSO asset: Assets > Create > ZeroEngine > Dialog > Dialog Graph
2. Double-click the asset to open in Dialog Graph Editor
3. Right-click canvas to add nodes
4. Drag from output port to input port to connect
5. Click node to edit properties in side panel
6. Save with Ctrl+S

---

### 2. BehaviorTree Graph Editor (v1.5.0+)

**Menu**: ZeroEngine > BehaviorTree > BT Graph Editor

Node-based visual editor for BTTreeAsset assets:
- Create and edit behavior trees visually
- Support for Composite, Decorator, and Leaf nodes
- Set root node designation
- Hierarchical layout visualization
- Build runtime BehaviorTree from asset

**Node Categories**:
| Category | Nodes |
|----------|-------|
| Composite | Sequence, Selector, Parallel |
| Decorator | Repeater, Inverter, Conditional, AlwaysSucceed, AlwaysFail |
| Leaf | ActionNode, WaitNode, LogNode |

**Usage**:
1. Create a BTTreeAsset: Assets > Create > ZeroEngine > BehaviorTree > BT Tree Asset
2. Double-click the asset to open in BT Graph Editor
3. Right-click canvas to add nodes
4. Connect parent nodes to child nodes
5. Right-click a node to set as Root
6. Save with Ctrl+S

**Runtime Usage**:
```csharp
using ZeroEngine.BehaviorTree;

public BTTreeAsset treeAsset;

void Start()
{
    // Create runtime tree from asset
    BehaviorTree tree = treeAsset.CreateRuntime();
    tree.SetOwner(gameObject);
    tree.Start();
}

void Update()
{
    tree.Tick(Time.deltaTime);
}
```

---

### 3. BehaviorTree Viewer (v1.4.0+)

**Menu**: ZeroEngine > BehaviorTree Viewer

Real-time visualization of behavior tree execution:
- View tree structure hierarchically
- Monitor node execution state (Running/Success/Failure)
- Track multiple trees simultaneously
- Expand/collapse composite nodes

**Usage in Code**:
```csharp
// Register tree for viewing
BehaviorTreeViewerWindow.RegisterTree(myBehaviorTree);

// Unregister when done
BehaviorTreeViewerWindow.UnregisterTree(myBehaviorTree);
```

---

### 4. Buff Editor

**Menu**: ZeroEngine > Buff Editor

Visual editor for BuffData ScriptableObjects:
- Create new buffs with auto-generated IDs
- Browse all buffs with icons
- Search by buff name
- Configure stat modifiers

**Requires**: Odin Inspector

---

### 5. Ability Editor

**Menu**: ZeroEngine > Tools > Ability Editor

Visual editor for AbilityDataSO (TCE pattern):
- Browse all abilities with icons
- Deep search (search by component type)
- Create new abilities
- Edit Triggers, Conditions, Effects lists

**Requires**: Odin Inspector

---

### 6. Inventory Editor

**Menu**: ZeroEngine > Inventory Editor

Visual editor for InventoryItemSO:
- Browse all items
- Create new items
- Configure item properties

**Requires**: Odin Inspector

---

### 7. Quest Editor

**Menu**: ZeroEngine > Quest Editor

Visual editor for quest data.

**Requires**: Odin Inspector

---

### 8. Global Search

**Menu**: ZeroEngine > Tools > Global Search (Ctrl+G)

Search across all ZeroEngine data assets.

---

## Dashboard

**Menu**: ZeroEngine > Dashboard

Unified entry point providing:
- Plugin status (Easy Save, Ink, etc.)
- Optional module status (Netcode, Spine)
- Quick access to all editor tools
- Debug utilities (clear saves, add items)

---

## Custom Editor Extensions

### Creating Custom Property Drawers

```csharp
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MyDataType))]
public class MyDataTypeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Custom drawing logic
    }
}
```

### Creating Custom Editor Windows

```csharp
using UnityEditor;
using UnityEngine;

public class MyEditorWindow : EditorWindow
{
    [MenuItem("ZeroEngine/My Tool")]
    public static void ShowWindow()
    {
        GetWindow<MyEditorWindow>("My Tool");
    }

    private void OnGUI()
    {
        // Editor GUI logic
    }
}
```

### With Odin Inspector

```csharp
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;

public class MyOdinWindow : OdinMenuEditorWindow
{
    [MenuItem("ZeroEngine/My Odin Tool")]
    private static void OpenWindow() => GetWindow<MyOdinWindow>();

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.AddAllAssetsAtPath("Items", "Assets/Data", typeof(MyDataSO), true);
        return tree;
    }
}
#endif
```

---

## File Structure

```
Editor/
├── ZeroEngineDashboard.cs      # Main dashboard window
├── GlobalSearchWindow.cs       # Global asset search
├── PluginManager.cs            # Plugin detection
├── README.md                   # This file
├── BehaviorTree/
│   ├── BehaviorTreeViewerWindow.cs  # BT runtime viewer
│   └── Graph/                  # v1.5.0+ GraphView editor
│       ├── BTGraphEditorWindow.cs   # BT graph main window
│       ├── BTGraphView.cs           # GraphView component
│       ├── BTGraphNode.cs           # Visual node classes
│       └── BTGraphStyles.uss        # Editor styles
├── Buff/
│   └── BuffEditorWindow.cs     # Buff data editor
├── Ability/
│   └── AbilityEditorWindow.cs  # Ability data editor
├── Inventory/
│   └── InventoryEditorWindow.cs
├── Quest/
│   └── QuestEditorWindow.cs
├── Dialog/
│   ├── DialogDebuggerEditor.cs
│   ├── DialogExportWindow.cs   # CSV export
│   └── Graph/                  # v1.5.0+ GraphView editor
│       ├── DialogGraphEditorWindow.cs  # Dialog graph main window
│       ├── DialogGraphView.cs          # GraphView component
│       ├── DialogGraphNode.cs          # Visual node classes (8 types)
│       ├── DialogNodeInspector.cs      # Node property panel
│       └── DialogGraphStyles.uss       # Editor styles
├── Audio/
│   └── AudioDebuggerEditor.cs
└── ModSystem/
    ├── ModCreatorWindow.cs
    ├── ModExporter.cs
    └── ModValidatorWindow.cs
```

---

## Dependencies

| Tool | Requires |
|------|----------|
| Dialog Graph Editor | None |
| BehaviorTree Graph Editor | None |
| BehaviorTree Viewer | None |
| Buff Editor | Odin Inspector |
| Ability Editor | Odin Inspector |
| Inventory Editor | Odin Inspector |
| Quest Editor | Odin Inspector |
| Dashboard | None |
| Global Search | None |

---

## Keyboard Shortcuts

| Shortcut | Context | Action |
|----------|---------|--------|
| Ctrl+G | Global | Open Global Search |
| Ctrl+D | Dialog/BT Graph | Duplicate selected node |
| Delete | Dialog/BT Graph | Delete selected node |
| Ctrl+S | Dialog/BT Graph | Save graph asset |
| Right-click | Dialog/BT Graph | Open add node menu |
