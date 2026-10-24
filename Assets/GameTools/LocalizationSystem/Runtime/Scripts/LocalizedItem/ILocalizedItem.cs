using UnityEngine;

namespace GameTools.LocalizationSystem
{
    public interface ILocalizedItem
    {
        public void OnLanguageUpdate(SystemLanguage language);
    }
}