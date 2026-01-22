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
        public static void Analyze(IDictionary dictionary, out DictionaryMetrics metrics)
        {
            metrics = new DictionaryMetrics();
            if (dictionary == null) return;

            metrics.Count = dictionary.Count;

            // 1. Determine Types safely
            var genericArguments = dictionary.GetType().GetGenericArguments();
            if (genericArguments.Length == 2)
            {
                metrics.KeyType = PrettifyType(genericArguments[0]);
                metrics.ValueType = PrettifyType(genericArguments[1]);
                metrics.IsReferenceType = !genericArguments[1].IsValueType;
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
            var nullCount = 0;

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value == null || entry.Value.Equals(null)) // Unity Object null check
                    nullCount++;

                // Heuristic: 16 bytes overhead + key + value (roughly)
                estimatedSize += 16;
            }

            metrics.NullValues = nullCount;
            metrics.ApproxSizeBytes = estimatedSize + metrics.Count * IntPtr.Size * 2;
        }

        public static void CopyJson(IDictionary dictionary)
        {
            if (dictionary == null) return;
            var jsonStringBuilder = new StringBuilder();
            jsonStringBuilder.Append("{\n");
            foreach (DictionaryEntry entry in dictionary)
            {
                var keyText = entry.Key?.ToString() ?? "null";
                var valueText = entry.Value?.ToString() ?? "null";
                jsonStringBuilder.Append($"  \"{keyText}\": \"{valueText}\",\n");
            }

            if (dictionary.Count > 0) jsonStringBuilder.Length -= 2; // remove trailing comma
            jsonStringBuilder.Append("\n}");
            GUIUtility.systemCopyBuffer = jsonStringBuilder.ToString();
            Debug.Log($"[DictionaryVisualizer] Copied {dictionary.Count} entries to clipboard.");
        }

        private static string PrettifyType(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(string)) return "string";
            if (type == typeof(GameObject)) return "GameObject";
            return type.Name;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / 1024.0 / 1024.0:F2} MB";
        }
    }
}