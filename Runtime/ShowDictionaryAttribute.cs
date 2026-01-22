using System;
using UnityEngine;

namespace AV.DictionaryVisualizer.Runtime
{
    public class ShowDictionaryAttribute : PropertyAttribute
    {
        public readonly string KeyFormatter;
        public readonly Type KeyFormatterType;
        public readonly string Title;

        public readonly string ValueFormatter;
        public readonly Type ValueFormatterType;

        public ShowDictionaryAttribute(
            string title = null,
            Type keyFormatterType = null,
            string keyFormatter = null,
            Type valueFormatterType = null,
            string valueFormatter = null
        )
        {
            Title = title;
            ValueFormatterType = valueFormatterType;
            ValueFormatter = valueFormatter;
            KeyFormatterType = keyFormatterType;
            KeyFormatter = keyFormatter;
        }
    }
}