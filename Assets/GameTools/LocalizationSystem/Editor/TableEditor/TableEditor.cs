using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GameTools.LocalizationSystem.Runtime;

namespace GameTools.LocalizationSystem.Editor
{
    public class TableEditor<TValue, TWindow> : UnityEditor.Editor where TWindow: LocalizationTableWindow<TValue>
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var table = (LocalizationTable<TValue>)target;
            var button = new Button(() => LocalizationTableWindow<TValue>.Open<TWindow>(table))
            {
                text = "Open in Window"
            };
            var fold = new Foldout
            {
                value = false,
                style = { marginTop = 10 }
            };
            InspectorElement.FillDefaultInspector(fold, serializedObject, this);
            root.Add(button);
            root.Add(fold);
            return root;
        }
    }
}