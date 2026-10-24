
using GameTools.LocalizationSystem.Runtime;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameTools.LocalizationSystem.Editor
{
    public class SpriteTableWindow: LocalizationTableWindow<Sprite>
    {
        protected override VisualElement MakeValueCell(SystemLanguage language)
        {
            var of = new ObjectField() {  objectType = typeof(Sprite) };
            SetMargin(of, 3);
            of.RegisterValueChangedCallback(value =>
            {
                var row = (LocalizedKeyValuePair<Sprite>)of.userData;
                row.SetValue(language, (Sprite)value.newValue);
                SetTargetDirty();
                SetBorder(of, false, 0);
            });
            return of;
        }

        protected override void BindValueCell(SystemLanguage language, VisualElement element, int i)
        {
            var of = (ObjectField)element;
            var translations = target.languageData[i];
            element.userData = translations;
            if (translations.TryGetValue(language, out var value))
            {
                of.SetValueWithoutNotify(value);
                SetBorder(of, false, 0);  
            }
            else
            {
                of.SetValueWithoutNotify(null);
                SetBorder(of, true, 1);  
            }
        }
    }
}