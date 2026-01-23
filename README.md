# AV.DictionaryVisualizer

![Header](documentation_header.svg)

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-000000.svg?style=flat-square&logo=unity)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE.md)

Inspector drawer for visualizing Dictionary contents in Unity.

## ✨ Features

- **Read-Only Visualization**: Inspect dictionary keys and values in Play Mode.
- **Formatted Display**: Clean table-like layout for dictionary entries.
- **Universal Support**: Works with `Dictionary<TKey, TValue>` for supported serializable types.

## 📦 Installation

Install via Unity Package Manager (git URL).

### Dependencies
- **Variable.RPG** (NuGet - Required for samples only)

## 🚀 Usage

Add the `[ShowDictionary]` attribute to your dictionary field.

```csharp
using AV.DictionaryVisualizer.Runtime;

[ShowDictionary]
public Dictionary<string, int> inventory;
```

## ⚠️ Status

- 🧪 **Tests**: Missing.
- 📘 **Samples**: None.
