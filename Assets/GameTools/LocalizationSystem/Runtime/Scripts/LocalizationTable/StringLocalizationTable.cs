using System;
using System.Collections.Generic;
using UnityEngine;
namespace GameTools.LocalizationSystem.Runtime
{
    [CreateAssetMenu(menuName = "ScriptableObjects/LocalizationTable/StringTable")]
    public class StringLocalizationTable: LocalizationTable<string>
    {
        protected override string ValuesOnNotFound(SystemLanguage language, string key) => key;
    }
}