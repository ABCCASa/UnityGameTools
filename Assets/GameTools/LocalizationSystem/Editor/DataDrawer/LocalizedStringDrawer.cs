using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using GameTools.LocalizationSystem.Runtime;

namespace GameTools.LocalizationSystem.Editor
{
    [CustomPropertyDrawer(typeof(LocalizedData<string>))]
    public class LocalizedStringDrawer : LocalizedDataDrawer<string, StringTable>
    {
        protected override void OpenTable(LocalizationTable<string> table)
        {
            StringTableWindow.Open<StringTableWindow>(table);
        }

        private protected override VisualElement DisplayValue(LocalizationTable<string> table, LocalizedKeyValuePair<string> row, SystemLanguage language)
        {
            var tf = new TextField(language.ToString(), -1, true, false, '*')
            {
                isDelayed = true
            };
          
            if (row.TryGetValue(language, out string value))
            {
                tf.SetValueWithoutNotify(value);
                SetBorder(tf[1], false, 1);
            }
            else
            {
                tf.SetValueWithoutNotify("");
                SetBorder(tf[1], true, 1);
            }
            
            tf.RegisterValueChangedCallback((v) =>
            {
                row.SetValue(language, v.newValue);
                EditorUtility.SetDirty(table);
                SetBorder(tf[1], false, 1);
            });
            return tf;
        }
    }
}