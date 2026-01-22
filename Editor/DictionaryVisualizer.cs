using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AV.DictionaryVisualizer.Editor;
using AV.DictionaryVisualizer.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AV.Tools.Editor
{
    /// <summary>
    ///     High-performance dictionary visualizer using reflection.
    ///     Renders dictionaries with professional styling matching Unity's design language.
    /// </summary>
    public static class DictionaryVisualizer
    {
        private static GUIStyle _headerStyle;
        private static GUIStyle _keyStyle;
        private static GUIStyle _valStyle;
        private static GUIStyle _boxStyle;
        private static readonly Color StripedRow = new(0, 0, 0, 0.05f);
        private static readonly Color BorderLine = new(0, 0, 0, 0.15f);
        private static readonly Dictionary<string, bool> Foldouts = new();
        private static readonly Dictionary<string, string> SearchTexts = new();
        private static readonly Dictionary<(Type, string), MethodInfo> _formatterCache = new();

        /// <summary>
        ///     Scans target for [ShowDictionary] attributes and renders them in Play Mode.
        ///     Call this in your CustomEditor's OnInspectorGUI() after base.OnInspectorGUI().
        /// </summary>
        public static void DrawDebugDictionaries(Object target)
        {
            if (!Application.isPlaying || target == null) return;
            CacheStyles();

            var fields = target.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var anyFound = false;

            foreach (var field in fields)
            {
                var showDictionaryAttribute = field.GetCustomAttribute<ShowDictionaryAttribute>();
                if (showDictionaryAttribute == null) continue;

                if (!anyFound)
                {
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("Dictionary Inspector", EditorStyles.boldLabel);
                    anyFound = true;
                }

                var dictionary = field.GetValue(target) as IDictionary;
                var uniqueId = $"{target.GetInstanceID()}_{field.Name}";
                var title = string.IsNullOrEmpty(showDictionaryAttribute.Title)
                    ? ObjectNames.NicifyVariableName(field.Name)
                    : showDictionaryAttribute.Title;

                RenderDictionary(uniqueId, title, dictionary, showDictionaryAttribute, target);
            }
        }

        private static void CacheStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };

            _keyStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 10,
                padding = new RectOffset(6, 2, 0, 0),
                clipping = TextClipping.Ellipsis
            };

            _valStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 10,
                padding = new RectOffset(2, 6, 0, 0),
                clipping = TextClipping.Ellipsis
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(0, 0, 1, 1)
            };
        }

        private static void RenderDictionary(string uniqueId, string title, IDictionary dictionary, ShowDictionaryAttribute showDictionaryAttribute,
            Object target)
        {
            if (!Foldouts.ContainsKey(uniqueId)) Foldouts[uniqueId] = true;
            if (!SearchTexts.ContainsKey(uniqueId)) SearchTexts[uniqueId] = "";

            DictionaryAnalytics.Analyze(dictionary, out var metrics);

            var headerRectangle = EditorGUILayout.GetControlRect(false, 22);

            if (Event.current.type == EventType.Repaint) _boxStyle.Draw(headerRectangle, false, false, false, false);

            var spacing = 6f;

            var copyButtonRectangle = new Rect(headerRectangle.xMax - 45, headerRectangle.y + 2, 40, 18);

            var statsWidth = 160f;
            var statsRectangle = new Rect(copyButtonRectangle.x - statsWidth - spacing, headerRectangle.y, statsWidth, headerRectangle.height);

            var foldoutRectangle = new Rect(headerRectangle.x, headerRectangle.y, statsRectangle.x - headerRectangle.x, headerRectangle.height);

            DrawHeaderStats(statsRectangle, metrics);

            if (GUI.Button(copyButtonRectangle, new GUIContent("Copy", "Copy JSON to Clipboard"), EditorStyles.miniButton))
                CopyJson(dictionary, showDictionaryAttribute, target);

            var label = $"{title} ({metrics.Count})";

            var originalColor = GUI.contentColor;
            if (metrics.NullValues > 0) GUI.contentColor = new Color(1f, 0.6f, 0.6f);

            Foldouts[uniqueId] = EditorGUI.Foldout(foldoutRectangle, Foldouts[uniqueId], label, true, _headerStyle);
            GUI.contentColor = originalColor;

            if (Foldouts[uniqueId])
            {
                EditorGUILayout.BeginVertical(_boxStyle);

                if (dictionary == null)
                {
                    EditorGUILayout.HelpBox("Dictionary is null", MessageType.Error);
                }
                else if (metrics.Count == 0)
                {
                    EditorGUILayout.HelpBox("Empty", MessageType.None);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    SearchTexts[uniqueId] = EditorGUILayout.TextField(SearchTexts[uniqueId],
                        GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarSearchField);
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_graph_close_h"),
                            GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.toolbarButton,
                            GUILayout.Width(18)))
                    {
                        SearchTexts[uniqueId] = "";
                        GUI.FocusControl(null);
                    }

                    GUILayout.Space(4);
                    EditorGUILayout.EndHorizontal();

                    RenderEntries(dictionary, SearchTexts[uniqueId], showDictionaryAttribute, target);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(2);
        }

        private static void DrawHeaderStats(Rect rectangle, DictionaryMetrics metrics)
        {
            var iconSize = 16f;
            var iconYPosition = rectangle.y + 3;
            var textYPosition = rectangle.y + 3;

            GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
            var memoryIcon = EditorGUIUtility.IconContent("Profiler.Memory");
            var memoryIconRectangle = new Rect(rectangle.xMax - 60, iconYPosition, iconSize, iconSize);
            var memoryTextRectangle = new Rect(memoryIconRectangle.x + iconSize + 2, textYPosition, 50, iconSize);

            GUI.Label(memoryIconRectangle, memoryIcon);
            GUI.Label(memoryTextRectangle, DictionaryAnalytics.FormatBytes(metrics.ApproxSizeBytes), EditorStyles.miniLabel);

            if (metrics.NullValues > 0)
            {
                GUI.contentColor = new Color(1f, 0.5f, 0.5f);
                var warningIcon = EditorGUIUtility.IconContent("console.warnicon.sml");

                var warningIconRectangle = new Rect(memoryIconRectangle.x - 75, iconYPosition, iconSize, iconSize);
                var warningTextRectangle = new Rect(warningIconRectangle.x + iconSize + 2, textYPosition, 60, iconSize);

                GUI.Label(warningIconRectangle, warningIcon);
                GUI.Label(warningTextRectangle, $"{metrics.NullValues} nulls", EditorStyles.miniBoldLabel);
            }
            else
            {
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                var typeRectangle = new Rect(rectangle.x, textYPosition, rectangle.width - 70, iconSize);
                var typeSignature = $"{metrics.KeyType} -> {metrics.ValueType}";
                GUI.Label(typeRectangle, typeSignature, _valStyle);
            }

            GUI.contentColor = Color.white;
        }

        private static void DrawStatsBar(DictionaryMetrics metrics, IDictionary dictionary)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label($"{metrics.KeyType} ➔ {metrics.ValueType}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(EditorGUIUtility.IconContent("Profiler.Memory"), GUILayout.Width(16), GUILayout.Height(16));
            GUILayout.Label(DictionaryAnalytics.FormatBytes(metrics.ApproxSizeBytes), EditorStyles.miniLabel);
            GUI.contentColor = Color.white;

            GUILayout.Space(8);

            if (metrics.NullValues > 0)
            {
                GUI.contentColor = new Color(1f, 0.5f, 0.5f);
                GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon.sml"), GUILayout.Width(16),
                    GUILayout.Height(16));
                GUILayout.Label($"{metrics.NullValues} nulls", EditorStyles.miniBoldLabel);
                GUI.contentColor = Color.white;
                GUILayout.Space(8);
            }

            if (GUILayout.Button(new GUIContent("Copy", "Copy Dictionary to Clipboard as JSON"),
                    EditorStyles.toolbarButton, GUILayout.Width(40)))
                DictionaryAnalytics.CopyJson(dictionary);

            EditorGUILayout.EndHorizontal();

            var separatorRectangle = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(separatorRectangle, new Color(0, 0, 0, 0.1f));
        }

        private static void RenderEntries(IDictionary dictionary, string searchText, ShowDictionaryAttribute showDictionaryAttribute,
            Object target)
        {
            var hasSearch = !string.IsNullOrEmpty(searchText);
            var searchLower = hasSearch ? searchText.ToLowerInvariant() : "";

            var rowIndex = 0;
            var matchCount = 0;

            foreach (DictionaryEntry entry in dictionary)
            {
                var keyText = StringifyKey(entry.Key, showDictionaryAttribute, target);
                var valueText = Stringify(entry.Value, showDictionaryAttribute, target);

                if (hasSearch)
                {
                    var keyMatch = keyText.ToLowerInvariant().Contains(searchLower);
                    var valueMatch = valueText.ToLowerInvariant().Contains(searchLower);
                    if (!keyMatch && !valueMatch) continue;
                }

                matchCount++;
                var rowRectangle = EditorGUILayout.GetControlRect(false, 18);

                if (rowIndex % 2 == 0) EditorGUI.DrawRect(rowRectangle, StripedRow);

                var borderRectangle = new Rect(rowRectangle.x, rowRectangle.yMax - 1, rowRectangle.width, 1);
                EditorGUI.DrawRect(borderRectangle, BorderLine);

                var keyWidth = rowRectangle.width * 0.5f;
                var keyRectangle = new Rect(rowRectangle.x, rowRectangle.y, keyWidth, rowRectangle.height);
                var valueRectangle = new Rect(rowRectangle.x + keyWidth, rowRectangle.y, keyWidth, rowRectangle.height);

                EditorGUI.LabelField(keyRectangle, new GUIContent(keyText, keyText), _keyStyle);
                EditorGUI.LabelField(valueRectangle, new GUIContent(valueText, valueText), _valStyle);

                rowIndex++;
            }

            if (hasSearch && matchCount == 0) EditorGUILayout.HelpBox("No matches found", MessageType.Info);
        }

        private static string Stringify(object value, ShowDictionaryAttribute showDictionaryAttribute = null, Object target = null)
        {
            if (value == null) return "null";

            if (showDictionaryAttribute != null && !string.IsNullOrEmpty(showDictionaryAttribute.ValueFormatter))
            {
                var valueType = value.GetType();
                var formatterSourceType = showDictionaryAttribute.ValueFormatterType ?? valueType;
                var cacheKey = (formatterSourceType, showDictionaryAttribute.ValueFormatter);

                if (!_formatterCache.TryGetValue(cacheKey, out var formatterMethod))
                {
                    if (showDictionaryAttribute.ValueFormatterType != null)
                        formatterMethod = formatterSourceType.GetMethod(showDictionaryAttribute.ValueFormatter,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    else
                        formatterMethod = formatterSourceType.GetMethod(showDictionaryAttribute.ValueFormatter,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    _formatterCache[cacheKey] = formatterMethod;
                }

                if (formatterMethod != null)
                    try
                    {
                        if (formatterMethod.IsStatic) return (string)formatterMethod.Invoke(null, new[] { value });

                        return (string)formatterMethod.Invoke(value, null);
                    }
                    catch
                    {
                        return "Err";
                    }
            }

            if (value is Object unityObject) return unityObject ? unityObject.name : "null";
            if (value is string stringValue) return stringValue;
            return value.ToString();
        }

        private static string StringifyKey(object key, ShowDictionaryAttribute showDictionaryAttribute = null, Object target = null)
        {
            if (key == null) return "null";

            if (showDictionaryAttribute != null && !string.IsNullOrEmpty(showDictionaryAttribute.KeyFormatter))
            {
                var formatterSourceType = showDictionaryAttribute.KeyFormatterType ?? target?.GetType();
                var cacheKey = (formatterSourceType, showDictionaryAttribute.KeyFormatter);

                if (!_formatterCache.TryGetValue(cacheKey, out var formatterMethod))
                {
                    if (showDictionaryAttribute.KeyFormatterType != null)
                        formatterMethod = formatterSourceType.GetMethod(showDictionaryAttribute.KeyFormatter,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    else if (target != null)
                        formatterMethod = formatterSourceType.GetMethod(showDictionaryAttribute.KeyFormatter,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    _formatterCache[cacheKey] = formatterMethod;
                }

                if (formatterMethod != null)
                    try
                    {
                        if (formatterMethod.IsStatic) return (string)formatterMethod.Invoke(null, new[] { key });

                        if (target != null) return (string)formatterMethod.Invoke(target, new[] { key });
                    }
                    catch
                    {
                        return "Err";
                    }
            }

            if (key is Object unityObject) return unityObject ? unityObject.name : "null";
            if (key is string keyValue) return keyValue;
            return key.ToString();
        }

        private static void CopyJson(IDictionary dictionary, ShowDictionaryAttribute showDictionaryAttribute, Object target)
        {
            if (dictionary == null) return;
            var jsonStringBuilder = new StringBuilder();
            jsonStringBuilder.Append("{\n");
            foreach (DictionaryEntry entry in dictionary)
            {
                var keyText = StringifyKey(entry.Key, showDictionaryAttribute, target);
                var valueText = Stringify(entry.Value, showDictionaryAttribute, target);
                jsonStringBuilder.Append($"  \"{keyText}\": \"{valueText}\",\n");
            }

            if (dictionary.Count > 0) jsonStringBuilder.Length -= 2; // remove trailing comma
            jsonStringBuilder.Append("\n}");
            GUIUtility.systemCopyBuffer = jsonStringBuilder.ToString();
            Debug.Log($"[DictionaryVisualizer] Copied {dictionary.Count} entries to clipboard.");
        }
    }
}