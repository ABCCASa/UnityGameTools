
using GameTools.LocalizationSystem.Runtime;
using UnityEngine;
using TMPro;
namespace GameTools.LocalizationSystem
{
    
    [DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
    public class LocalizedText: MonoBehaviour, ILocalizedItem
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private LocalizedData<string> localizedString;
        public void OnLanguageUpdate(SystemLanguage language)
        {
            if (text == null)
            {
                Debug.LogWarning($"text is not set", gameObject);
                return;
            }
            text.text = localizedString[language];
        }

        private void OnEnable()
        {
            LocalizationSystem.RegisterItem(this);  
        }

        private void OnDisable()
        {
            LocalizationSystem.UnregisterItem(this);
        }

        void Reset()
        {
            text = GetComponent<TMP_Text>();
        }
    }
}