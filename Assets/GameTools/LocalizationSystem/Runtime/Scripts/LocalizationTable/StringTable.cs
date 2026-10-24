
using UnityEngine;
namespace GameTools.LocalizationSystem.Runtime
{
    [CreateAssetMenu(menuName = "ScriptableObjects/LocalizationTable/StringTable")]
    public class StringTable: LocalizationTable<string>
    {
        protected override string ValuesForNotFound(SystemLanguage language, string key)
        {
            return key;
        }
    }
}