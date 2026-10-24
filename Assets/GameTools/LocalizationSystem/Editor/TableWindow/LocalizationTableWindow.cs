using System;
using GameTools.LocalizationSystem.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameTools.LocalizationSystem.Editor
{
    public abstract class LocalizationTableWindow<TValue> : EditorWindow
    {
        protected LocalizationTable<TValue> target;
        private MultiColumnListView table;

        public static void Open<TWindow>(LocalizationTable<TValue> target) where TWindow : LocalizationTableWindow<TValue>
        {
            var wnd = GetWindow<TWindow>();
            wnd.target = target;
            target.ValidateData();
            wnd.SetTargetDirty();
            wnd.titleContent = new GUIContent("Localization Editor");
            wnd.Init();
        }


        protected static void SetBorder(VisualElement element, bool warning, int width)
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
        protected static void SetMargin(VisualElement element, int width)
        {
            element.style.marginBottom = width;
            element.style.marginTop = width;
            element.style.marginLeft = width;
            element.style.marginRight = width;
        }

        protected void SetTargetDirty()
        {
            if (target != null) EditorUtility.SetDirty(target);
            else rootVisualElement.Clear();
        }

        public void Init()
        {
            var root = rootVisualElement;
            root.Clear();
            if (target == null) return;
            table = new MultiColumnListView
            {
                itemsSource = target.languageData,
                showBoundCollectionSize = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                horizontalScrollingEnabled = true,
                showAddRemoveFooter = true,
                selectionType = SelectionType.Multiple,
                sortingEnabled = true,
                style = { flexShrink = 1, marginRight = 5, marginLeft = 5 },
                reorderable = true,
                columns = { reorderable = false }
            };
            table.itemIndexChanged += (sender, e) => { SetTargetDirty(); };
            table.itemsAdded += (indexes) =>
            {
                int index = 0;
                foreach (var i in indexes)
                {
                    string GetText()
                    {
                        return index == 0 ? "new_text_key" : $"new_text_key_{index}";
                    }

                    while (target.languageData.Exists(x => x != null && x.textKey == GetText()))
                    {
                        index++;
                    }

                    target.languageData[i] = new LocalizedKeyValuePair<TValue>(GetText());
                    SetTargetDirty();
                    table.RefreshItems();
                }
            };

            table.headerContextMenuPopulateEvent += (evt, column) =>
            {
                evt.menu.ClearItems();
                foreach (SystemLanguage languageOption in Enum.GetValues(typeof(SystemLanguage)))
                {
                    if (target.supportedLanguages.Contains(languageOption))
                    {
                        evt.menu.AppendAction(
                            $"Support Language/{languageOption.ToString()}", _ =>
                            {
                                foreach (var item in table.columns)
                                {
                                    if (item.title != languageOption.ToString()) continue;
                                    table.columns.Remove(item);
                                    target.supportedLanguages.Remove(languageOption);
                                    SetTargetDirty();
                                    return;
                                }
                            }, DropdownMenuAction.Status.Checked
                        );
                    }
                    else
                    {
                        evt.menu.AppendAction(
                            $"Support Language/{languageOption.ToString()}", _ =>
                            {
                                if (target.supportedLanguages.Contains(languageOption)) return;
                                target.supportedLanguages.Add(languageOption);
                                SetTargetDirty();
                                AddLanguageColumn(languageOption);
                            }
                        );
                    }
                }
            };

            #region keyColumn

            var keyColumn = new Column()
            {
                stretchable = false,
                width = 150,
                resizable = true,
                optional = false,
                sortable = true
            };

            table.columnSortingChanged += () =>
            {
                foreach (var item in table.sortedColumns)
                {
                    if (item.column != keyColumn) return;
                    target.languageData.Sort((a, b) =>
                    {
                        int result = string.Compare(a.textKey, b.textKey, StringComparison.CurrentCulture);
                        return item.direction == SortDirection.Ascending ? result : -result;
                    });
                    SetTargetDirty();
                    table.RefreshItems();
                    return;
                }
            };

            keyColumn.title = "Key";
            keyColumn.makeCell = () =>
            {
                var row = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, flexGrow = 1 }
                };

                var handle = new Label("≡")
                {
                    style = { width = 24, unityTextAlign = TextAnchor.MiddleCenter, opacity = 0.6f },
                    focusable = false
                };
                handle.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Delete", _ =>
                    {
                        target.languageData.Remove((LocalizedKeyValuePair<TValue>)row.userData);
                        SetTargetDirty();
                        table.RefreshItems();
                    });
                }));

                var tf = new TextField
                {
                    isDelayed = true,
                    style = { fontSize = 10, flexGrow = 1, whiteSpace = WhiteSpace.Normal }
                };
                SetMargin(tf, 3);
                tf.RegisterValueChangedCallback(value =>
                {
                    var bindData = (LocalizedKeyValuePair<TValue>)row.userData;
                    var newValue = value.newValue.Trim();
                    if (newValue != bindData.textKey && !string.IsNullOrWhiteSpace(newValue) && !target.languageData.Exists(d => d.textKey == newValue))
                    {
                        bindData.textKey = newValue;
                        SetTargetDirty();
                        tf.SetValueWithoutNotify(newValue);
                    }
                    else
                    {
                        tf.SetValueWithoutNotify(bindData.textKey);
                    }
                });
                row.Add(handle);
                row.Add(tf);
                return row;
            };

            keyColumn.bindCell = (element, i) =>
            {
                TextField tf = (TextField)element[1];
                LocalizedKeyValuePair<TValue> bindData = target.languageData[i];
                tf.SetValueWithoutNotify(bindData.textKey);
                element.userData = bindData;
            };
            table.columns.Add(keyColumn);

            #endregion

            #region languageColumn

            void AddLanguageColumn(SystemLanguage language)
            {
                var column = new Column
                {
                    stretchable = true,
                    resizable = true,
                    title = language.ToString(),
                    sortable = false,
                    makeCell = () => MakeValueCell(language),
                    bindCell = (element, i) => BindValueCell(language, element, i)
                };
                table.columns.Add(column);
            }

            foreach (var language in target.supportedLanguages)
            {
                AddLanguageColumn(language);
            }

            #endregion languageColumn

            #region search

            var searchRow = new VisualElement { style = { marginBottom = 10, marginTop = 10, flexDirection = FlexDirection.Row } };
            var searchField = new TextField { style = { flexGrow = 1 } };
            searchRow.Add(searchField);
            var searchButton = new Button(() =>
            {
                var prompt = searchField.value;
                if (string.IsNullOrEmpty(prompt))
                {
                    return;
                }

                int index = table.selectedIndex;
                if (target.TryFindNextMatch(prompt, ref index))
                {
                    table.ScrollToItem(index);
                    table.SetSelection(index);
                }
                else
                {
                    table.SetSelection(-1);
                }
            })
            {
                text = "Search",
                style = { height = 20 }
            };
            searchRow.Add(searchButton);

            #endregion

            root.Add(searchRow);
            root.Add(table);
        }


        protected abstract VisualElement MakeValueCell(SystemLanguage language);
        protected abstract void BindValueCell(SystemLanguage language, VisualElement element, int i);
       

        private void OnFocus()
        {
            if (target != null)
            {
                table.RefreshItems();
            }
        }
    }
}