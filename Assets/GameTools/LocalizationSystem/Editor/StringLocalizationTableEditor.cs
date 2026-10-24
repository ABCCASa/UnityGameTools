using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GameTools.LocalizationSystem;
using GameTools.LocalizationSystem.Runtime;

namespace GameTools.LocalizationSystem.Editor
{
    [CustomEditor(typeof(StringLocalizationTable))]
    public class StringLocalizationTableEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var button = new Button(() => StringLocalizationTableEditorWindow.Open((StringLocalizationTable)target))
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