using System;
using UnityEngine.Assertions;

namespace GameTools.UISystem
{
    public abstract class ContainerBase
    {
        public abstract int count { get; }
        public bool isBusy { get; private set; } = false;
        public bool isActive { get; private set; } = true;
        public abstract T Open<T>(float fadeTime = -1) where T : Screen;
        public abstract TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1) where TScreen : Screen<TParam>;
        public abstract void Close(ScreenBase screen, float fadeTime = -1f);
        public abstract void CloseAll(float fadeTime = -1f);
        public void SetActive(bool active)
        {
            if(isActive == active) return;
            isActive = active;
            if (active) OnResume(); 
            else OnPause();
        }
        private protected abstract void OnResume();
        private protected abstract void OnPause();
    
        internal abstract void UpdateOrder(ref int order);
        internal abstract void UpdateInteractable(ref bool interactable);
        protected void BusyBlock()
        {
            if (isBusy) throw new InvalidOperationException($"container {this} is busy");
        }
        
        protected IDisposable GetBusyScope() => new BusyScope(this);
        private sealed class BusyScope : IDisposable
        {
            private bool disposed;
            private readonly ContainerBase container;
            public BusyScope(ContainerBase container)
            {
                Assert.IsNotNull(container);
                if (container.isBusy) throw new InvalidOperationException($"{container} is busy");
                container.isBusy = true;
                this.container = container;
            }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                Assert.IsTrue(container.isBusy); 
                container.isBusy= false;
            }
        }

        
    }
}