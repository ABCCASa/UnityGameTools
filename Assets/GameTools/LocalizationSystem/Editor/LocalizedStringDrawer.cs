using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using GameTools.LocalizationSystem.Runtime;
using UnityEditor.UIElements;

namespace GameTools.LocalizationSystem.Editor
{
    
    [CustomPropertyDrawer(typeof(LocalizedString))]
    public class LocalizedStringDrawer : PropertyDrawer
    {
        private bool isFold = true;
        private VisualElement detailContainer;
        private ObjectField tableField;
        private TextField keyField;
        private SerializedProperty keyProperty, tableProperty;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            InitProperty(property);
            var root = new VisualElement();
            VisualElement basicContainer = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginLeft = 20 } };
            detailContainer = new VisualElement() { style = { marginLeft = 20 } };
            tableProperty = property.FindPropertyRelative("table");
            keyProperty = property.FindPropertyRelative("key");

            tableField = new ObjectField("")
            {
                objectType = typeof(StringLocalizationTable),
                bindingPath = tableProperty.propertyPath,
                style = { flexGrow = 0, flexShrink = 2 },
            };

            tableField.RegisterValueChangedCallback(value =>
            {
                var table = value.newValue as StringLocalizationTable;
                var key = keyProperty.stringValue;
                OnValueChange(table, key);
            });

            keyField = new TextField
            {
                isDelayed = true,
                bindingPath = keyProperty.propertyPath,
                style = { flexGrow = 2, flexShrink = 1 },
            };

            keyField.RegisterValueChangedCallback(value =>
            {
                string newValue = value.newValue.Trim();
                if (newValue != value.newValue) 
                {
                    keyField.SetValueWithoutNotify(newValue);
                    property.serializedObject.Update();
                    keyProperty.stringValue = newValue;
                    property.serializedObject.ApplyModifiedProperties();
                }
                OnValueChange(tableProperty.objectReferenceValue as StringLocalizationTable, newValue);
            });
            
            keyField.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.AppendSeparator();
                property.serializedObject.Update();
                var t = tableProperty.objectReferenceValue as StringLocalizationTable;
                if (t == null)
                    return;

                if (t.languageData.Count == 0)
                {
                    evt.menu.AppendAction($"Paths", null, DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction($"Keys", null, DropdownMenuAction.Status.Disabled);
                }
                else
                {
                    foreach (var kv in t.languageData)
                    {
                        evt.menu.AppendAction($"Paths/{kv.textKey.Replace('.', '/')}", _ =>
                        {
                            property.serializedObject.Update();
                            keyProperty.stringValue = kv.textKey;
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    
                        evt.menu.AppendAction($"Keys/{kv.textKey}", _ =>
                        {
                            property.serializedObject.Update();
                            keyProperty.stringValue = kv.textKey;
                            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        });
                    }
                }
            });
            
            var foldButton = new Button
            {
                text = "◂",
                style = { width = 20, height = 20, marginRight = 0 }
            };
            
            foldButton.clicked += () =>
            {  
                property.serializedObject.Update();
                isFold = !isFold;
                if (isFold)
                {
                    foldButton.text = "◂";
                    detailContainer.Clear();
                }
                else
                {
                    foldButton.text = "▾";
                    UpdateDetail(tableProperty.objectReferenceValue as StringLocalizationTable, keyProperty.stringValue);
                }
            };
            
            Button openTableBtn = new Button(() =>
            {  
                property.serializedObject.Update();
                var table = tableProperty.objectReferenceValue as StringLocalizationTable;
                if (table!= null) 
                { StringLocalizationTableWindow.Open<StringLocalizationTableWindow>(table); }
            }) {  text = "Open" };
            basicContainer.Add(tableField);
            basicContainer.Add(openTableBtn);
            basicContainer.Add(keyField);
            basicContainer.Add(foldButton);
            root.Add(new Label(property.displayName) { style = { marginLeft = 5 } });
            root.Add(basicContainer);
            root.Add(detailContainer);
            ValidationTest(tableProperty.objectReferenceValue as StringLocalizationTable, keyProperty.stringValue);
            return root;
        }

        private void OnValueChange(StringLocalizationTable table, string key)
        { 
            ValidationTest(table, key);
            if (!isFold) UpdateDetail(table, key);
        }

        private void ValidationTest(StringLocalizationTable table, string key)
        { 
            bool tableError = table == null;
            bool keyError = tableError || string.IsNullOrWhiteSpace(key) || !table.languageData.Exists(x => x.textKey == key);
            SetBorder(tableField.contentContainer[0], tableError, 1);
            SetBorder(keyField[0], keyError, 1);
        }
        
        private void AddKeyToTable(StringLocalizationTable table, string key)
        {
            if (table !=null && !table.languageData.Exists(t => t.textKey == key))
            {
                table.languageData.Add(new LocalizedKeyValuePair<string>(key));
                EditorUtility.SetDirty(table);
            }
            OnValueChange(table, key); 
        }
        
        private void UpdateDetail(StringLocalizationTable table, string key)
        {
            detailContainer.Clear();
            if (table == null || string.IsNullOrWhiteSpace(key)) { return; }
            var row = table.languageData.Find(t => t.textKey == key);
            if (row == null)
            {
                detailContainer.Add(new Button(() => AddKeyToTable(table, key) ) { text = "Create" });
                return;
            }
            foreach (var l in table.supportedLanguages)
            {
                var tf = new TextField(l.ToString(), -1, true, false, '*')
                {
                    isDelayed = true
                };
          
                if (row.TryGetValue(l, out string value))
                {
                    tf.SetValueWithoutNotify(value);
                    SetBorder(tf[1], false, 1);
                }
                else
                {
                    tf.SetValueWithoutNotify("");
                    SetBorder(tf[1], true, 1);
                }

                detailContainer.Add(tf);
                tf.RegisterValueChangedCallback((v) =>
                {
                    row.SetValue(l, v.newValue);
                    EditorUtility.SetDirty(table);
                    SetBorder(tf[1], false, 1);
                });
            }
        }

        private static void InitProperty(SerializedProperty property)
        { 
            property.serializedObject.Update();
            var isInitProperty = property.FindPropertyRelative("isInit");
            var tableProperty = property.FindPropertyRelative("table");
            if (isInitProperty.boolValue)
                return;
            isInitProperty.boolValue = true;
            if (tableProperty.objectReferenceValue ==null)
            {
                var defaultTable = Resources.Load<StringLocalizationTable>("LocalizationTable/Default");
                if (defaultTable != null) tableProperty.objectReferenceValue = defaultTable;
            }
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBorder(VisualElement element, bool warning, int width)
        {
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            if (warning)
            {
                element.style.borderBottomColor = Color.red;
                element.style.borderRightColor = Color.red;
                element.style.borderTopColor = Color.red;
                element.style.borderLeftColor = Color.red;
            }
            else
            {
                element.style.borderBottomColor = StyleKeyword.Null;
                element.style.borderRightColor = StyleKeyword.Null;
                element.style.borderTopColor = StyleKeyword.Null;
                element.style.borderLeftColor = StyleKeyword.Null;
            }
        }
        
    }
}