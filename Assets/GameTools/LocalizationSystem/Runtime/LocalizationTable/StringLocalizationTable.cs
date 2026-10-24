using System;
using System.Collections.Generic;
using UnityEngine;
namespace GameTools.LocalizationSystem.Runtime
{
    [CreateAssetMenu(menuName = "ScriptableObjects/LocalizationTable")]
    public class StringLocalizationTable: LocalizationTableBase<string>
    {
        protected override string ValuesOnNotFound(SystemLanguage language, string key) => key;
    }
}