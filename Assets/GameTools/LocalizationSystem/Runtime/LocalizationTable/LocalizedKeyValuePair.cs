using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTools.LocalizationSystem.Runtime
{
    [Serializable]
    internal class LocalizedKeyValuePair<TValue>
    {
        public string textKey;
        [SerializeField] protected List<Translation<TValue>> translations = new();

        public LocalizedKeyValuePair(string textKey)
        {
            this.textKey = textKey; 
        }

        public bool TryGetValue(SystemLanguage language, out TValue value)
        {
            var result = translations.Find(x => x.language ==  language);
            if (result != null)
            {
               value = result.value;
               return true;
            }
            value = default;
            return false; 
        }

        public void SetValue(SystemLanguage language, TValue value)
        {
            var result = translations.Find(x => x.language ==  language);
            if(result != null)  result.value = value;
            else translations.Add(new Translation<TValue>(language, value));
        }

        public virtual bool MatchSearchPrompt(string prompt, List<SystemLanguage> existLanguages)
        {
            if (textKey.Contains(prompt)) return true;
           
            return false;
        }

        /// <summary> 确保语言不重复 </summary>
        internal void ValidateData()
        {
            HashSet<SystemLanguage> validLanguages = new();
            for (int i = translations.Count - 1; i >= 0; i--)
            {
                var trans = translations[i];
                if (!validLanguages.Add(trans.language))
                {
                    translations.RemoveAt(i);
                }
            }
        }
        
        /// <summary> 确保语言不重复, 并且移除不在范围内的语言 </summary>
        internal void ValidateData(List<SystemLanguage> supportedLanguage)
        {
            HashSet<SystemLanguage> validLanguages = new();
            for (int i = translations.Count - 1; i >= 0; i--)
            {
                var trans = translations[i];
                if (!supportedLanguage.Contains(trans.language) || !validLanguages.Add(trans.language))
                {
                    translations.RemoveAt(i);
                }
            }
        }
    }

    [Serializable]
    internal class Translation<TValue>
    { 
        public SystemLanguage language;
        public TValue value;
        public Translation(SystemLanguage language, TValue value)
        {
            this.language = language;
            this.value = value;
        }
    }
}