using System;
using UnityEngine;

namespace GameTools.LocalizationSystem.Runtime
{
    [Serializable]
    public struct LocalizedString
    {
        #pragma warning disable CS0414
        [SerializeField, HideInInspector] private bool isInit; //判断是否初始化，在editor下使用，如果否则会使用默认的table赋值
        #pragma warning restore CS0414
        [SerializeField] private StringLocalizationTable table;
        [SerializeField] private string key;
        public string value => this[LocalizationSystem.Language];
        public string this[SystemLanguage language] => table == null ? throw new ArgumentNullException(nameof(table)) : table[language, key];
        public LocalizedString(StringLocalizationTable table, string key)
        {
            isInit = true;
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }
            if(string.IsNullOrEmpty(key)) 
            {
                throw new ArgumentNullException(nameof(key));
            }
            this.table = table;
            this.key = key;
        }
    } 
}


