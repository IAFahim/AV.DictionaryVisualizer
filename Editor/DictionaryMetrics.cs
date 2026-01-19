using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace AV.DictionaryVisualizer.Editor
{
    // LAYER A: Pure Data
    public struct DictionaryMetrics
    {
        public int Count;
        public int NullValues;
        public string KeyType;
        public string ValueType;
        public long ApproxSizeBytes;
        public bool IsReferenceType;
    }

    // LAYER B: Core Logic (Stateless Analysis)
    public static class DictionaryAnalytics
    {
        public static void Analyze(IDictionary dict, out DictionaryMetrics metrics)
        {
            metrics = new DictionaryMetrics();
            if (dict == null) return;

            metrics.Count = dict.Count;

            // 1. Determine Types safely
            var genericArgs = dict.GetType().GetGenericArguments();
            if (genericArgs.Length == 2)
            {
                metrics.KeyType = PrettifyType(genericArgs[0]);
                metrics.ValueType = PrettifyType(genericArgs[1]);
                metrics.IsReferenceType = !genericArgs[1].IsValueType;
            }
            else
            {
                metrics.KeyType = "Obj";
                metrics.ValueType = "Obj";
                metrics.IsReferenceType = true;
            }

            // 2. Linear Scan for Nulls and Size Heuristic
            // Note: We avoid deep reflection here to keep the Editor responsive.
            long estimatedSize = 0;
            var nulls = 0;

            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Value == null || entry.Value.Equals(null)) // Unity Object null check
                    nulls++;

                // Heuristic: 16 bytes overhead + key + value (roughly)
                estimatedSize += 16;
            }

            metrics.NullValues = nulls;
            metrics.ApproxSizeBytes = estimatedSize + metrics.Count * IntPtr.Size * 2;
        }

        public static void CopyJson(IDictionary dict)
        {
            if (dict == null) return;
            var sb = new StringBuilder();
            sb.Append("{\n");
            foreach (DictionaryEntry entry in dict)
            {
                var k = entry.Key?.ToString() ?? "null";
                var v = entry.Value?.ToString() ?? "null";
                sb.Append($"  \"{k}\": \"{v}\",\n");
            }

            if (dict.Count > 0) sb.Length -= 2; // remove trailing comma
            sb.Append("\n}");
            GUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log($"[DictionaryVisualizer] Copied {dict.Count} entries to clipboard.");
        }

        private static string PrettifyType(Type t)
        {
            if (t == typeof(int)) return "int";
            if (t == typeof(float)) return "float";
            if (t == typeof(string)) return "string";
            if (t == typeof(GameObject)) return "GameObject";
            return t.Name;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / 1024.0 / 1024.0:F2} MB";
        }
    }
}