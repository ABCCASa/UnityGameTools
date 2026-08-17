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
        public bool isInitialized { get; private set; }
        public ScreenContainer screenContainer 
        {
            get
            {
                if(!isInitialized) Initialize();
                return _screenContainer;
            }
        }
        
        public void Initialize()
        {
            if(isInitialized) return;
            isInitialized = true;
            _screenContainer = new ScreenContainer(loader);
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                var screen = child.GetComponent<ScreenBase>();
                if (screen != null) { loader.RegisterScreen(screen); }
            }
            LayerManager.AddContainer(_screenContainer, layerOrder);
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
            LayerManager.RemoveContainer(screenContainer);
        }
    }
}