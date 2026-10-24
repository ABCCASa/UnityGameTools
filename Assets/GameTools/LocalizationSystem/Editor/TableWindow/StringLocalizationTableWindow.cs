
using System;
using GameTools.LocalizationSystem.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameTools.LocalizationSystem.Editor
{
    public class StringLocalizationTableWindow : LocalizationTableWindow<string>
    {
        protected override VisualElement MakeValueCell(SystemLanguage language)
        {
            var tf = new TextField
            {
                multiline = true,
                isDelayed = true,
                style = { fontSize = 10, flexGrow = 1, whiteSpace = WhiteSpace.Normal, minHeight = 30 }
            };
            SetMargin(tf, 3);
            tf.RegisterValueChangedCallback(value =>
            {
                var row = ( LocalizedKeyValuePair<string>)tf.userData;
                row.SetValue(language, value.newValue);
                SetTargetDirty();
                SetBorder(tf, false, 0);
            });
            return tf;
        }

        protected override void BindValueCell(SystemLanguage language, VisualElement element, int i)
        {
            TextField tf = (TextField)element;
            var translations = target.languageData[i];
            element.userData = translations;
            if (translations.TryGetValue(language, out var value))
            {
                tf.SetValueWithoutNotify(value);
                SetBorder(tf, false, 0);  
            }
            else
            {
                tf.SetValueWithoutNotify("");
                SetBorder(tf, true, 1);  
            }
        }
    }
}