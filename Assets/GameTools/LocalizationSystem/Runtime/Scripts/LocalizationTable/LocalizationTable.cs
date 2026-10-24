using System;
using System.Collections.Generic;
using UnityEngine;
namespace GameTools.LocalizationSystem.Runtime
{

    public abstract class LocalizationTable<TValue> : ScriptableObject
    {
        #region Runtime
        [SerializeField] internal List<LocalizedKeyValuePair<TValue>> languageData = new();
        [SerializeField] internal List<SystemLanguage> supportedLanguages = new();
        private Dictionary<string, TValue> runtimePack = null;
        private SystemLanguage? currentLanguage = null;
        public TValue this[SystemLanguage language, string key]
        {
            get
            {
                if (TryGetValue(language, key, out TValue value)) return value;
                return ValuesOnNotFound(language, key);
            }
        }

        // 缓存语言并返回需要的值，但一次只能缓存一种语言
        public bool TryGetValue(SystemLanguage language, string key, out TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (!supportedLanguages.Contains(language))
            {
                Debug.LogWarning($"[{name}] Unsupported language: {language}");
                value = default;
                return false;
            }
            if (currentLanguage != language || runtimePack == null)
            {
                currentLanguage = language;
                runtimePack = GetLocalizationDictionary(language);
            }
            if (runtimePack.TryGetValue(key, out value)) return true;
            Debug.LogWarning($"[{name}: {language}] Key not found: {key}");
            return false;
        }

        protected abstract TValue ValuesOnNotFound(SystemLanguage language, string key);
        
        private void OnEnable()
        {
            runtimePack = null;
        }
        
        private Dictionary<string, TValue> GetLocalizationDictionary(SystemLanguage language)
        {
            if (!supportedLanguages.Contains(language)) return null;
            Dictionary<string, TValue> result = new();
            foreach (var pair in languageData)
            {
                if (pair.TryGetValue(language, out TValue value))
                {
                    result.Add(pair.textKey, value); 
                }
            }
            return result;
        }
        #endregion
        
        #region Editor
        internal bool TryFindNextMatch(string prompt, ref int index)
        {
            if (string.IsNullOrEmpty(prompt))return false;
            for (index += 1; index < languageData.Count; index++)
            {
                if (languageData[index].MatchSearchPrompt(prompt, supportedLanguages)) return true;
            }
            return false;
        }
        
        internal void ValidateData()
        {
            // 检查重复的SystemLanguage
            HashSet<SystemLanguage> validLanguages = new();
            for (int i = supportedLanguages.Count - 1; i >= 0; i--)
            {
                var language = supportedLanguages[i];
                if (!validLanguages.Add(language))
                {
                    supportedLanguages.RemoveAt(i);
                }
            }
            
            // 检查重复的key
            HashSet<string> validKeys = new();
            for (int i = languageData.Count - 1; i >= 0; i--)
            {
                var pair = languageData[i];
                if (validKeys.Add(pair.textKey))
                {
                    pair.ValidateData(); 
                }
                else
                {
                    languageData.RemoveAt(i);
                    Debug.LogError($"[{name}] Key repeat: {pair.textKey}");
                }
            }
        }
        #endregion
    }
}