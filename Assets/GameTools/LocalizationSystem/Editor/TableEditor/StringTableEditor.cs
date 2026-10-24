using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GameTools.LocalizationSystem.Runtime;

namespace GameTools.LocalizationSystem.Editor
{
    [CustomEditor(typeof(StringTable))]
    public class StringTableEditor : TableEditor<string, StringTableWindow>
    {
      
    }
}