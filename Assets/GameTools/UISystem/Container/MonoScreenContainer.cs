using System;
using UnityEngine;
namespace GameTools.UISystem
{
    public class MonoScreenContainer: MonoBehaviour
    {
        [SerializeField] private bool initOnAwake = true;
        [field: SerializeField] public int layerOrder { get; private set; }
        private readonly RegistryScreenLoader loader = new();
        private ScreenContainer _screenContainer;
        public ScreenContainer screenContainer {
            get
            {
                if(!isInitialized) Initialize();
                return _screenContainer;
            }
        }
        
        public bool isInitialized { get; private set; } = false;
        public void Initialize()
        {
            if(isInitialized) return;
            isInitialized = true;
            _screenContainer = new ScreenContainer(loader);
            UIManager.AddContainer(_screenContainer, layerOrder);
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                var screen = child.GetComponent<ScreenBase>();
                if (screen != null) { loader.RegisterScreen(screen); }
            }
        }

        private void Awake()
        {
            if(initOnAwake) Initialize();
        }

        private void OnDestroy()
        {
            if (_screenContainer == null) return;
            _screenContainer.CloseAll();
            loader.Dispose();
            UIManager.RemoveContainer(screenContainer);
        }
    }
}