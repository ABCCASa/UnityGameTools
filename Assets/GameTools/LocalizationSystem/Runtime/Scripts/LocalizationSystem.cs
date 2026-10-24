using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameTools.LocalizationSystem
{
    public static class LocalizationSystem
    {
        public static SystemLanguage Language { get; private set; } = SystemLanguage.ChineseSimplified;
        private static readonly HashSet<ILocalizedItem> LocalizedItems = new();
        
        public static void SetLanguage(SystemLanguage language)
        {
            if (Language == language) { return; }
            Language = language;
            foreach (var item in LocalizedItems.ToList())
            {
                item.OnLanguageUpdate(Language);
            }
        }

        public static bool RegisterItem(ILocalizedItem item)
        {
            if (LocalizedItems.Add(item))
            {
                item.OnLanguageUpdate(Language);
                return true;
            }
            return false;
        }

        public static bool UnregisterItem(ILocalizedItem item)
        {
            return LocalizedItems.Remove(item);
        }
    }
}