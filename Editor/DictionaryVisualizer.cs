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
                var attr = field.GetCustomAttribute<ShowDictionaryAttribute>();
                if (attr == null) continue;

                if (!anyFound)
                {
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("Dictionary Inspector", EditorStyles.boldLabel);
                    anyFound = true;
                }

                var dict = field.GetValue(target) as IDictionary;
                var uid = $"{target.GetInstanceID()}_{field.Name}";
                var title = string.IsNullOrEmpty(attr.Title)
                    ? ObjectNames.NicifyVariableName(field.Name)
                    : attr.Title;

                RenderDictionary(uid, title, dict, attr, target);
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

        private static void RenderDictionary(string uid, string title, IDictionary dict, ShowDictionaryAttribute attr,
            Object target)
        {
            if (!Foldouts.ContainsKey(uid)) Foldouts[uid] = true;
            if (!SearchTexts.ContainsKey(uid)) SearchTexts[uid] = "";

            DictionaryAnalytics.Analyze(dict, out var metrics);

            var headerRect = EditorGUILayout.GetControlRect(false, 22);

            if (Event.current.type == EventType.Repaint) _boxStyle.Draw(headerRect, false, false, false, false);

            var spacing = 6f;

            var copyRect = new Rect(headerRect.xMax - 45, headerRect.y + 2, 40, 18);

            var statsWidth = 160f;
            var statsRect = new Rect(copyRect.x - statsWidth - spacing, headerRect.y, statsWidth, headerRect.height);

            var foldoutRect = new Rect(headerRect.x, headerRect.y, statsRect.x - headerRect.x, headerRect.height);

            DrawHeaderStats(statsRect, metrics);

            if (GUI.Button(copyRect, new GUIContent("Copy", "Copy JSON to Clipboard"), EditorStyles.miniButton))
                CopyJson(dict, attr, target);

            var label = $"{title} ({metrics.Count})";

            var originalColor = GUI.contentColor;
            if (metrics.NullValues > 0) GUI.contentColor = new Color(1f, 0.6f, 0.6f);

            Foldouts[uid] = EditorGUI.Foldout(foldoutRect, Foldouts[uid], label, true, _headerStyle);
            GUI.contentColor = originalColor;

            if (Foldouts[uid])
            {
                EditorGUILayout.BeginVertical(_boxStyle);

                if (dict == null)
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
                    SearchTexts[uid] = EditorGUILayout.TextField(SearchTexts[uid],
                        GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarSearchField);
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_graph_close_h"),
                            GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.toolbarButton,
                            GUILayout.Width(18)))
                    {
                        SearchTexts[uid] = "";
                        GUI.FocusControl(null);
                    }

                    GUILayout.Space(4);
                    EditorGUILayout.EndHorizontal();

                    RenderEntries(dict, SearchTexts[uid], attr, target);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(2);
        }

        private static void DrawHeaderStats(Rect rect, DictionaryMetrics metrics)
        {
            var iconSize = 16f;
            var iconY = rect.y + 3;
            var textY = rect.y + 3;

            GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
            var memIcon = EditorGUIUtility.IconContent("Profiler.Memory");
            var memIconRect = new Rect(rect.xMax - 60, iconY, iconSize, iconSize);
            var memTextRect = new Rect(memIconRect.x + iconSize + 2, textY, 50, iconSize);

            GUI.Label(memIconRect, memIcon);
            GUI.Label(memTextRect, DictionaryAnalytics.FormatBytes(metrics.ApproxSizeBytes), EditorStyles.miniLabel);

            if (metrics.NullValues > 0)
            {
                GUI.contentColor = new Color(1f, 0.5f, 0.5f);
                var warnIcon = EditorGUIUtility.IconContent("console.warnicon.sml");

                var warnIconRect = new Rect(memIconRect.x - 75, iconY, iconSize, iconSize);
                var warnTextRect = new Rect(warnIconRect.x + iconSize + 2, textY, 60, iconSize);

                GUI.Label(warnIconRect, warnIcon);
                GUI.Label(warnTextRect, $"{metrics.NullValues} nulls", EditorStyles.miniBoldLabel);
            }
            else
            {
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                var typeRect = new Rect(rect.x, textY, rect.width - 70, iconSize);
                var typeSig = $"{metrics.KeyType} -> {metrics.ValueType}";
                GUI.Label(typeRect, typeSig, _valStyle);
            }

            GUI.contentColor = Color.white;
        }

        private static void DrawStatsBar(DictionaryMetrics metrics, IDictionary dict)
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
                DictionaryAnalytics.CopyJson(dict);

            EditorGUILayout.EndHorizontal();

            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.1f));
        }

        private static void RenderEntries(IDictionary dict, string searchText, ShowDictionaryAttribute attr,
            Object target)
        {
            var hasSearch = !string.IsNullOrEmpty(searchText);
            var searchLower = hasSearch ? searchText.ToLowerInvariant() : "";

            var idx = 0;
            var matchCount = 0;

            foreach (DictionaryEntry entry in dict)
            {
                var keyText = StringifyKey(entry.Key, attr, target);
                var valText = Stringify(entry.Value, attr, target);

                if (hasSearch)
                {
                    var keyMatch = keyText.ToLowerInvariant().Contains(searchLower);
                    var valMatch = valText.ToLowerInvariant().Contains(searchLower);
                    if (!keyMatch && !valMatch) continue;
                }

                matchCount++;
                var rect = EditorGUILayout.GetControlRect(false, 18);

                if (idx % 2 == 0) EditorGUI.DrawRect(rect, StripedRow);

                var borderRect = new Rect(rect.x, rect.yMax - 1, rect.width, 1);
                EditorGUI.DrawRect(borderRect, BorderLine);

                var split = rect.width * 0.5f;
                var keyRect = new Rect(rect.x, rect.y, split, rect.height);
                var valRect = new Rect(rect.x + split, rect.y, split, rect.height);

                EditorGUI.LabelField(keyRect, new GUIContent(keyText, keyText), _keyStyle);
                EditorGUI.LabelField(valRect, new GUIContent(valText, valText), _valStyle);

                idx++;
            }

            if (hasSearch && matchCount == 0) EditorGUILayout.HelpBox("No matches found", MessageType.Info);
        }

        private static string Stringify(object obj, ShowDictionaryAttribute attr = null, Object target = null)
        {
            if (obj == null) return "null";

            if (attr != null && !string.IsNullOrEmpty(attr.ValueFormatter))
            {
                var targetType = obj.GetType();
                var sourceType = attr.ValueFormatterType ?? targetType;
                var cacheKey = (sourceType, attr.ValueFormatter);

                if (!_formatterCache.TryGetValue(cacheKey, out var method))
                {
                    if (attr.ValueFormatterType != null)
                        method = sourceType.GetMethod(attr.ValueFormatter,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    else
                        method = sourceType.GetMethod(attr.ValueFormatter,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    _formatterCache[cacheKey] = method;
                }

                if (method != null)
                    try
                    {
                        if (method.IsStatic) return (string)method.Invoke(null, new[] { obj });

                        return (string)method.Invoke(obj, null);
                    }
                    catch
                    {
                        return "Err";
                    }
            }

            if (obj is Object unityObj) return unityObj ? unityObj.name : "null";
            if (obj is string str) return str;
            return obj.ToString();
        }

        private static string StringifyKey(object obj, ShowDictionaryAttribute attr = null, Object target = null)
        {
            if (obj == null) return "null";

            if (attr != null && !string.IsNullOrEmpty(attr.KeyFormatter))
            {
                var sourceType = attr.KeyFormatterType ?? target?.GetType();
                var cacheKey = (sourceType, attr.KeyFormatter);

                if (!_formatterCache.TryGetValue(cacheKey, out var method))
                {
                    if (attr.KeyFormatterType != null)
                        method = sourceType.GetMethod(attr.KeyFormatter,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    else if (target != null)
                        method = sourceType.GetMethod(attr.KeyFormatter,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    _formatterCache[cacheKey] = method;
                }

                if (method != null)
                    try
                    {
                        if (method.IsStatic) return (string)method.Invoke(null, new[] { obj });

                        if (target != null) return (string)method.Invoke(target, new[] { obj });
                    }
                    catch
                    {
                        return "Err";
                    }
            }

            if (obj is Object unityObj) return unityObj ? unityObj.name : "null";
            if (obj is string str) return str;
            return obj.ToString();
        }

        private static void CopyJson(IDictionary dict, ShowDictionaryAttribute attr, Object target)
        {
            if (dict == null) return;
            var sb = new StringBuilder();
            sb.Append("{\n");
            foreach (DictionaryEntry entry in dict)
            {
                var k = StringifyKey(entry.Key, attr, target);
                var v = Stringify(entry.Value, attr, target);
                sb.Append($"  \"{k}\": \"{v}\",\n");
            }

            if (dict.Count > 0) sb.Length -= 2; // remove trailing comma
            sb.Append("\n}");
            GUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log($"[DictionaryVisualizer] Copied {dict.Count} entries to clipboard.");
        }
    }
}