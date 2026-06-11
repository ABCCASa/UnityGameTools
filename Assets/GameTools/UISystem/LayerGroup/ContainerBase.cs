namespace GameTools.UISystem
{
    public abstract class ContainerBase
    {
        public bool isActive { get; private set; } = true;
        public abstract T Open<T>(float fadeTime = -1) where T : Screen;
        public abstract TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1) where TScreen : Screen<TParam>;
        public abstract void Close(ScreenBase screen, float fadeTime = -1f);
        public abstract void CloseAll();

        public void Resume()
        {
            if(isActive) return;
            isActive = true;
            OnResume();
        }

        public void Pause()
        {
            if(!isActive) return;
            isActive = false;
            OnPause();
        }
        
        private protected abstract void OnResume();
        private protected abstract void OnPause();
        internal abstract void UpdateOrder(ref int order);
        internal abstract void UpdateInteractable(ref bool interactable);
    }
}