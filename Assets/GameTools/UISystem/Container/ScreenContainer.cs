using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace GameTools.UISystem
{
    public class ScreenContainer
    {
        private class RuntimeScreen
        {
            public readonly ScreenBase screen;
            public bool isSelfPause;
            public RuntimeScreen(ScreenBase screen)
            {
                this.screen = screen;
            }
        }
        private readonly IScreenLoader screenLoader;
      
        private List<RuntimeScreen> screenList = new();
        public int count => screenList.Count;
        public bool isBusy { get; private set; } = false;
        public bool isActive { get; private set; } = true;
        internal ScreenContainer(IScreenLoader screenLoader = null)
        {
            this.screenLoader = screenLoader ?? ResourcesScreenLoader.Instance;
        }
        
        private RuntimeScreen GetRuntimeScreen(ScreenBase screen)
        {
            return screenList.Find(item => item.screen == screen);
        }

        private bool Contains(ScreenBase screen)
        {
            return screenList.Exists(item => item.screen == screen);
        }

        private int IndexOf(ScreenBase screen)
        {
            return screenList.FindIndex(item => item.screen == screen);
        }

        private sealed class BusyScope : IDisposable
        {
            private bool disposed;
            private readonly ScreenContainer container;
            public BusyScope(ScreenContainer container)
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
                container.isBusy = false;
            }
        }

        protected void BusyBlock()
        {
            if (isBusy) throw new InvalidOperationException($"container {this} is busy");
        }

        protected IDisposable GetBusyScope() => new BusyScope(this);

        private TScreen OpenBase<TScreen>(Action<TScreen> openAction, bool addAbove, ScreenBase relative) where TScreen : ScreenBase
        {
            using (GetBusyScope())
            {
                if(!isActive) throw new InvalidOperationException($"{this} is not active");
                if (relative != null)
                {
                    if (relative.state == ScreenState.Close) throw new Exception($"{relative} is already close, cannot use as relative screen");
                    if (!Contains(relative)) throw new Exception($"{relative} is not include in this Group");
                }
                var screen = screenLoader.GetScreen<TScreen>();

                RuntimeScreen runtimeScreen = new(screen);
                if (relative == null)
                {
                    if (addAbove) screenList.Add(runtimeScreen);
                    else screenList.Insert(0, runtimeScreen);
                }
                else
                {
                    int index = IndexOf(relative);
                    screenList.Insert(addAbove ? index + 1 : index, runtimeScreen);
                }
                openAction.Invoke(screen);
                LayerManager.UpdateInteractable();
                LayerManager.UpdateOrder();
                return screen;
            }
        }

        public TScreen Open<TScreen>(float fadeTime = -1, string animKey = null, bool addAbove = true, ScreenBase relative = null) where TScreen : Screen
        {
            return OpenBase<TScreen>(screen => screen.SetOpen(fadeTime, this, animKey), addAbove, relative);
        }

        public TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1, string animKey = null, bool addAbove = true, ScreenBase relative = null) where TScreen : Screen<TParam>
        {
            return OpenBase<TScreen>(screen => screen.SetOpen(param, fadeTime, this, animKey), addAbove, relative);
        }

        public void Pause(ScreenBase screen, float fadeTime = -1, string animKey = null)
        {
            using (GetBusyScope())
            {
                RuntimeScreen runtimeScreen = GetRuntimeScreen(screen);
                if(runtimeScreen == null) throw new ArgumentException($"{screen} is not include in this Group");
                if(isActive) screen.SetPause(fadeTime, animKey, LayerManager.UpdateInteractable);
                else if (runtimeScreen.isSelfPause) throw new InvalidOperationException("cannot pause screen when state is not open"); 
                runtimeScreen.isSelfPause = true;
            }
        }
        
        public void Resume(ScreenBase screen, float fadeTime = -1, string animKey = null)
        {
            using (GetBusyScope())
            {   
                RuntimeScreen runtimeScreen = GetRuntimeScreen(screen);
                if (runtimeScreen == null) throw new ArgumentException($"{screen} is not include in this Group");
                if (isActive)
                {
                    screen.SetResume(fadeTime, animKey);
                    LayerManager.UpdateInteractable();
                    LayerManager.UpdateOrder(); 
                }
                else if (!runtimeScreen.isSelfPause) throw new InvalidOperationException("cannot resume screen when state is not pause");
                runtimeScreen.isSelfPause = false;
            }
        }

        public void Close(ScreenBase screen, float fadeTime = -1f, string animKey = null)
        {
            using (GetBusyScope())
            {
                RuntimeScreen runtimeScreen = GetRuntimeScreen(screen);
                if (runtimeScreen == null) throw new ArgumentException($"{screen} is not include in this Group");
                screen.SetClose(fadeTime, animKey, callback: () =>
                {
                    screenList.Remove(runtimeScreen);
                    screenLoader.ReleaseScreen(screen);
                    LayerManager.UpdateInteractable();
                });
            }
        }

        public void ChangeOrder(ScreenBase target, bool addAbove = true, ScreenBase relative = null)
        {
            using (GetBusyScope())
            {
                if(target == relative) throw new ArgumentException($"target: {target} is same as relative: {relative}");
                
                if (target == null) throw new ArgumentNullException(nameof(target));
                if (target.state == ScreenState.Close) throw new Exception($"{target} is already close, cannot change order");
                
                var runtimeTarget = GetRuntimeScreen(target);
                if (runtimeTarget == null) throw new ArgumentException($"{target} is not include in this Group");
                
                if (relative != null)
                {
                    if (relative.state == ScreenState.Close) throw new Exception($"{relative} is already close, cannot use as relative screen");
                    if (!Contains(relative)) throw new ArgumentException($"{relative} is not include in this Group");
                }
                screenList.Remove(runtimeTarget);
                if (relative == null)
                {
                    if (addAbove) screenList.Add(runtimeTarget);
                    else screenList.Insert(0, runtimeTarget);
                }
                else
                {
                    int index = IndexOf(relative);
                    screenList.Insert(addAbove ? index + 1 : index, runtimeTarget);
                }
                LayerManager.UpdateInteractable();
                LayerManager.UpdateOrder();
            }
        }
        
        
        public void SetActive()
        {
            using (GetBusyScope())
            {
                if(isActive) throw new Exception($"{this} is already active");
                isActive = true;
                
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    RuntimeScreen runtimeScreen = screenList[i];
                    ScreenBase screen = runtimeScreen.screen;
                    Assert.IsTrue(screen.state == ScreenState.Pause);// 因为 container 处于inactive状态，所以全部是pause的
                    if(runtimeScreen.isSelfPause) continue;
                    screen.SetResume();
                }
                LayerManager.UpdateInteractable();
                LayerManager.UpdateOrder();
            }
        }


        public void SetInactive()
        {
            using (LayerManager.GetDelayScope())
            using (GetBusyScope())
            {
                if(!isActive) throw new InvalidOperationException($"{this} is not active");
                isActive = false;
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    ScreenBase screen = screenList[i].screen;
                    if (screen.state == ScreenState.Open) screen.SetPause(callback: LayerManager.UpdateInteractable);
                    else if (screen.isFade) screen.CompleteAnimation();
                }
            }
        }

        public void CloseAll(float fadeTime = -1f, string animKey = null)
        {
            using (GetBusyScope())
            {
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    var runtimeScreen = screenList[i];
                    var screen = runtimeScreen.screen;
                    if (screen.state == ScreenState.Close)
                    {
                        screen.SpeedUpAnimation(fadeTime); // 加快动画
                    }
                    else
                    {
                        screen.SetClose(fadeTime, animKey, callback: () =>
                        {
                            screenList.Remove(runtimeScreen);
                            screenLoader.ReleaseScreen(screen);
                            LayerManager.UpdateInteractable();
                        });
                    }
                }
            }
        }

        public void SpeedUpAnimations(float fadeTime)
        {
            using (GetBusyScope())
            {
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    var screen = screenList[i].screen;
                    if (screen.isFade) screen.SpeedUpAnimation(fadeTime);
                    else Assert.IsFalse(screen.state == ScreenState.Close);
                }
            }
        }

        public void CompleteAnimations()
        {
            using (GetBusyScope())
            {
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    var screen = screenList[i].screen;
                    if (screen.isFade) screen.CompleteAnimation();
                    else Assert.IsFalse(screen.state == ScreenState.Close);
                }
            }
        }

        internal void UpdateInteractable(ref bool interactable)
        {
            if(!isActive) return;
            for (int i = screenList.Count - 1; i >= 0; i--)
            {
                IContainerItem item = screenList[i].screen;
                item.UpdateInteractable(ref interactable);
            }
        }

        internal void UpdateOrder(ref int order)
        {
            if(!isActive) return;
            for (int i = 0; i < screenList.Count; i++)
            {
                IContainerItem screen = screenList[i].screen;
                screen.UpdateOrder(ref order);
            }
           
        }
    }
}