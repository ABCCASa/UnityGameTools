using GameTools.LocalizationSystem.Runtime;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace GameTools.LocalizationSystem
{
    
    [DisallowMultipleComponent, RequireComponent(typeof(Image))]
    public class LocalizedImage: MonoBehaviour, ILocalizedItem
    {
        [SerializeField] private Image image;
        [SerializeField] private LocalizedData<Sprite> localizedSprite;
        public void OnLanguageUpdate(SystemLanguage language)
        {
            if (image == null)
            {
                Debug.LogWarning($"image is not set", gameObject);
                return;
            }
            image.sprite = localizedSprite[language];
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
            image = GetComponent<Image>();
        }
    }
}