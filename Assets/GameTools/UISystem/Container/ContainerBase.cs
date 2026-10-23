using System;

namespace GameTools.UISystem
{
    public enum ContainerState { Init, Active, Inactive, Dispose }
    public abstract class ContainerBase
    {
        private protected readonly ReentrancyGuard reentrancyGuard = new();
        private protected IScreenLoader screenLoader;
        private protected ContainerState state { get; private set; } = ContainerState.Init;
        private bool isRunning => state is ContainerState.Active or ContainerState.Inactive;
        public abstract int count { get; }
        internal void Initialize(IScreenLoader screenLoader = null)
        {
            if (state != ContainerState.Init) throw new InvalidOperationException();
            state = ContainerState.Active;
            this.screenLoader = screenLoader ?? ResourcesScreenLoader.Instance;
        }
        
        private protected abstract void OnInActive();
        private protected abstract void OnActive();
        private protected abstract void OnDispose();

        public void Active()
        {
            using (reentrancyGuard.Enter())
            {
                if(!isRunning) throw new InvalidOperationException("container is disposed or uninitialized");
                if (state != ContainerState.Inactive) return;
                state = ContainerState.Active;
                OnActive();
            }
        }

        public void Inactive()
        {
            using (reentrancyGuard.Enter())
            {
                if (!isRunning) throw new InvalidOperationException("container is disposed or uninitialized");
                if (state != ContainerState.Active) return;
                state = ContainerState.Inactive;
                OnInActive();
            }
        }
        
        internal void Dispose()
        {
            using (reentrancyGuard.Enter())
            {
                if (!isRunning) throw new InvalidOperationException("container is disposed or uninitialized");
                state = ContainerState.Dispose;
                OnDispose();
                screenLoader.Dispose();
                screenLoader = null;
            }
        }
        
        internal abstract void UpdateInteractable(ref bool interactable);
        internal abstract void UpdateOrder(ref int order);
    }
}