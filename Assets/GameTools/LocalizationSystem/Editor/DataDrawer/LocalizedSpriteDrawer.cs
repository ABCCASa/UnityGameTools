using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using GameTools.LocalizationSystem.Runtime;
using UnityEditor.UIElements;


namespace GameTools.LocalizationSystem.Editor
{
    [CustomPropertyDrawer(typeof(LocalizedData<Sprite>))]
    public class LocalizedSpriteDrawer : LocalizedDataDrawer<Sprite, SpriteTable>
    {
        protected override void OpenTable(LocalizationTable<Sprite> table)
        {
            SpriteTableWindow.Open<SpriteTableWindow>(table);
        }

        private protected override VisualElement DisplayValue(LocalizationTable<Sprite> table, LocalizedKeyValuePair<Sprite> row, SystemLanguage language)
        {
            var of = new ObjectField(language.ToString())
            {
                objectType = typeof(Sprite),
            };
          
            if (row.TryGetValue(language, out Sprite value))
            {
                of.SetValueWithoutNotify(value);
                SetBorder(of[1], false, 1);
            }
            else
            {
                of.SetValueWithoutNotify(null);
                SetBorder(of[1], true, 1);
            }
            
            of.RegisterValueChangedCallback((v) =>
            {
                row.SetValue(language, (Sprite)v.newValue);
                EditorUtility.SetDirty(table);
                SetBorder(of[1], false, 1);
            });
            return of;
        }
    }
}