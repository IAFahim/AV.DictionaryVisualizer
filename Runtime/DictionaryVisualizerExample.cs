using System.Collections.Generic;
using AV.DictionaryVisualizer.Runtime;
using UnityEngine;
using Variable.RPG;

[HelpURL("https://github.com/IAFahim/AV.DictionaryVisualizer")]
[AddComponentMenu("AV/DictionaryVisualizer/DictionaryVisualizerExample")]

/// <summary>
///     Example demonstrating the [ShowDictionary] attribute.
///     Enter Play Mode to see dictionaries visualized in the Inspector.
/// </summary>
public class DictionaryVisualizerExample : MonoBehaviour
{
    [ShowDictionary("Inventory Items")] private readonly Dictionary<string, int> _items = new();

    [ShowDictionary] private readonly Dictionary<GameObject, float> _objectWeights = new();

    [ShowDictionary("Equipment Slots")] private readonly Dictionary<int, GameObject> _slots = new();

    [ShowDictionary("Inventory Items")] private readonly Dictionary<int, RpgStat> _statsDictionary = new();

    private void Start()
    {
        var rpgStat = new RpgStat();
        _statsDictionary[0] = rpgStat;
        var s = rpgStat.ToStringCompact();
        _items["Health Potion"] = 5;
        _items["Mana Potion"] = 3;
        _items["Gold"] = 9999;
        _items["Sword of Truth"] = 1;

        _slots[0] = gameObject;
        _slots[1] = null;

        _objectWeights[gameObject] = 10.5f;
    }

    private void Update()
    {
        // Dynamic updates work in real-time
        if (Input.GetKeyDown(KeyCode.Space)) _items["Gold"] = _items.GetValueOrDefault("Gold") + 100;
    }
}