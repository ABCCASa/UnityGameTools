
using UnityEngine;
namespace GameTools.LocalizationSystem.Runtime
{
    [CreateAssetMenu(menuName = "ScriptableObjects/LocalizationTable/SpriteTable")]
    public class SpriteTable: LocalizationTable<Sprite>
    {
        protected override Sprite ValuesForNotFound(SystemLanguage language, string key)
        {
            return null;
        }
    }
}