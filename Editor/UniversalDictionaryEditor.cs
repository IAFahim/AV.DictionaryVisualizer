using UnityEditor;
using UnityEngine;

namespace AV.Tools.Editor
{
    /// <summary>
    ///     Universal dictionary visualizer that hooks into any MonoBehaviour.
    ///     Add this to your project once and all [ShowDictionary] attributes work automatically.
    ///     WARNING: This overrides ALL MonoBehaviour inspectors. If you have custom editors,
    ///     add DictionaryVisualizer.DrawDebugDictionaries(target) manually to those editors instead.
    /// </summary>
    [HelpURL("https://github.com/IAFahim/AV.DictionaryVisualizer")]
    [AddComponentMenu("AV/DictionaryVisualizer/UniversalDictionaryEditor")]
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    public class UniversalDictionaryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DictionaryVisualizer.DrawDebugDictionaries(target);
        }
    }
}