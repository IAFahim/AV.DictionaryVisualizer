# AV.DictionaryVisualizer

**Universal Dictionary Inspector for Unity**  
View any `Dictionary<TKey, TValue>` in the Inspector during Play Mode without writing custom editors.

---

## Features

✅ **Zero boilerplate** — Just add `[ShowDictionary]` to any dictionary field  
✅ **Works with ANY types** — `<string, int>`, `<GameObject, float>`, etc.  
✅ **Real-time updates** — See changes instantly during Play Mode  
✅ **Compact design** — Clean, professional styling like RpgStats  
✅ **Automatic integration** — Works on all MonoBehaviours automatically  

---

## Quick Start

### 1. Mark Your Dictionary

```csharp
using AV.Tools;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [ShowDictionary("Backpack Items")]
    private Dictionary<string, int> _items = new();
    
    private void Start()
    {
        _items["Potion"] = 5;
        _items["Gold"] = 9999;
    }
}
```

### 2. Enter Play Mode

The Inspector will show:

```
Dictionary Inspector
━━━━━━━━━━━━━━━━━━━━━━━━━━━
▼ Backpack Items (2)
  ┌────────────┬──────────┐
  │ Potion     │        5 │
  ├────────────┼──────────┤
  │ Gold       │     9999 │
  └────────────┴──────────┘
```

---

## Advanced Usage

### Custom Editor Integration

If you already have a custom editor, manually add one line:

```csharp
using AV.Tools.Editor;
using UnityEditor;

[CustomEditor(typeof(MyScript))]
public class MyScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        // Call this to render all dictionaries marked with [ShowDictionary] attribute
        DictionaryVisualizer.DrawDebugDictionaries(target); // ← Add this
    }
}
```

### Title Customization

```csharp
[ShowDictionary("Player Stats")]     // Custom title
private Dictionary<string, float> _stats;

[ShowDictionary]                     // Auto-generated title
private Dictionary<int, string> _ids;
```

### Supported Types

- **Primitives**: `int`, `float`, `string`, `bool`
- **Unity Objects**: `GameObject`, `Transform`, `ScriptableObject`
- **Custom Types**: Any type with a meaningful `ToString()`

---

## Architecture

### Files

```
AV.DictionaryVisualizer/
├── Runtime/
│   ├── ShowDictionaryAttribute.cs      ← The attribute
│   ├── DictionaryVisualizerExample.cs  ← Example script
│   └── AV.DictionaryVisualizer.Runtime.asmdef
└── Editor/
    ├── DictionaryVisualizer.cs         ← Core rendering logic
    ├── UniversalDictionaryEditor.cs    ← Auto-integration
    └── AV.DictionaryVisualizer.Editor.asmdef
```

### How It Works

1. **`[ShowDictionary]` attribute** marks dictionary fields for visualization
2. **`UniversalDictionaryEditor`** automatically hooks into all MonoBehaviour inspectors
3. **Reflection** finds marked dictionaries at runtime (Play Mode only)
4. **`DictionaryVisualizer`** renders dictionary contents using `EditorGUILayout` with professional styling
5. **`DictionaryAnalytics`** analyzes dictionary metrics (count, nulls, memory usage) for display

---

## Performance

- **No overhead** in builds (Editor-only code)
- **Reflection runs once per frame** per object (negligible)
- **Safe** — Only active during Play Mode in Editor

---

## Limitations

⚠️ **Play Mode Only** — Reflection requires runtime instances  
⚠️ **Read-Only** — Values cannot be edited (by design for safety)  
⚠️ **Editor-Only** — Not serialized, won't persist after Play Mode  

---

## Comparison to Alternatives

| Approach | Boilerplate | Type-Safe | Auto-Updates |
|----------|-------------|-----------|--------------|
| `[ShowDictionary]` | ✅ None | ✅ Yes | ✅ Yes |
| Custom PropertyDrawer | ❌ High | ✅ Yes | ❌ No |
| Custom Editor | ❌ Medium | ✅ Yes | ⚠️ Manual |
| Serialized Dictionary | ❌ Very High | ⚠️ Limited | ✅ Yes |

---

## Troubleshooting

### "Dictionary Not Showing"
✅ Enter Play Mode (Reflection requires runtime)
✅ Check attribute: `[ShowDictionary]`
✅ Verify namespace: `using AV.Tools;`

### "Conflicts with Custom Editor"
✅ Add `DictionaryVisualizer.DrawDebugDictionaries(target)` manually
✅ Or remove `UniversalDictionaryEditor.cs` and integrate directly

### "Naming Conventions"
This package follows strict naming guidelines from AGENTS.md:
- ✅ Full words: `dictionary` (not `dict`), `attribute` (not `attr`)
- ✅ Pronounceable: `valueRectangle` (not `valRect`)
- ✅ Searchable: `formatterMethod` (not `method` in ambiguous contexts)
- ✅ Clear intent: `rowIndex` (not `idx` or `i`)

---

## Credits

Built with the same architecture as **AV.RpgStats** for visual consistency.  
Created by IAFahim, 2026.
