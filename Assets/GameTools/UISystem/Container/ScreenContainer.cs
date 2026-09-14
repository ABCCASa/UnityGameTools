using System;
using System.Collections.Generic;

using UnityEngine.Assertions;

namespace GameTools.UISystem
{
    public class ScreenContainer: ContainerBase
    {
        private class RuntimeScreen
        {
            public readonly ScreenBase screen;
            public bool isSelfPause;
            public RuntimeScreen(ScreenBase screen) { this.screen = screen; }
        }
        private List<RuntimeScreen> screenList = new();
        public override int count => screenList.Count;
        
       
        private RuntimeScreen GetRuntimeScreen(ScreenBase screen) => screenList.Find(item => item.screen == screen);
        private bool Contains(ScreenBase screen) => screenList.Exists(item => item.screen == screen);
        private int IndexOf(ScreenBase screen) => screenList.FindIndex(item => item.screen == screen);
        private TScreen OpenBase<TScreen>(Action<TScreen> openAction, bool addAbove, ScreenBase relative) where TScreen : ScreenBase
        {
            using (reentrancyGuard.Enter())
            {
                if (state != ContainerState.Active) throw new InvalidOperationException($"{this} is not active, cannot open screen: {typeof(TScreen)}");
                if (relative != null)
                {
                    if (relative.state == ScreenState.Close) throw new ArgumentException($"{relative} is already close, cannot use as relative screen");
                    if (!Contains(relative)) throw new ArgumentException($"{relative} is not include in this Group");
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
            using (reentrancyGuard.Enter())
            {
                RuntimeScreen runtimeScreen = GetRuntimeScreen(screen);
                if (runtimeScreen == null) throw new ArgumentException($"{screen} is not include in this Group");
                if (state == ContainerState.Active) screen.SetPause(fadeTime, animKey, LayerManager.UpdateInteractable);
                else if (runtimeScreen.isSelfPause) throw new InvalidOperationException("cannot pause screen when state is not open");
                runtimeScreen.isSelfPause = true;
            }
        }

        public void Resume(ScreenBase screen, float fadeTime = -1, string animKey = null)
        {
            using (reentrancyGuard.Enter())
            {
                RuntimeScreen runtimeScreen = GetRuntimeScreen(screen);
                if (runtimeScreen == null) throw new ArgumentException($"{screen} is not include in this Group");
                if (state == ContainerState.Active)
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
            using (reentrancyGuard.Enter())
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
            using (reentrancyGuard.Enter())
            {
                if (target == relative) throw new ArgumentException($"target: {target} is same as relative: {relative}");

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
        
        private protected override void OnInActive()
        {
            using (LayerManager.GetDelayScope())
            {
                Assert.IsTrue(state == ContainerState.Inactive);
                if(screenList.Count == 0) return;
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    ScreenBase screen = screenList[i].screen;
                    if (screen.state == ScreenState.Open) screen.SetPause(callback: LayerManager.UpdateInteractable);
                    else if (screen.isFade) screen.CompleteAnimation();
                }
            }
        }
        private protected override void OnActive()
        {
            Assert.IsFalse(state == ContainerState.Inactive);
            if(screenList.Count == 0) return;
            for (int i = screenList.Count - 1; i >= 0; i--)
            {
                RuntimeScreen runtimeScreen = screenList[i];
                ScreenBase screen = runtimeScreen.screen;
                Assert.IsTrue(screen.state == ScreenState.Pause); // 因为 container 处于inactive状态，所以全部是pause的
                if (runtimeScreen.isSelfPause) continue;
                screen.SetResume();
            }
            LayerManager.UpdateInteractable();
            LayerManager.UpdateOrder();
        }
        
        private void OnCloseAll(float fadeTime = -1f, string animKey = null)
        {
            using (LayerManager.GetDelayScope())
            {
                if(screenList.Count == 0) return;
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    var runtimeScreen = screenList[i];
                    var screen = runtimeScreen.screen;
                    if (screen.state == ScreenState.Close)
                    {
                        screen.SpeedUpAnimation(fadeTime); // speed up animation
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
        
        public void CloseAll(float fadeTime = -1f, string animKey = null)
        {
            using (reentrancyGuard.Enter())
            {
                OnCloseAll(fadeTime, animKey);
            }
        }

        private protected override void OnDispose()
        {
            OnCloseAll();
        }

        public void SpeedUpAnimations(float fadeTime)
        {
            using (reentrancyGuard.Enter())
            {
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    screenList[i].screen.SpeedUpAnimation(fadeTime);
                }
            }
        }

        public void CompleteAnimations()
        {
            using (reentrancyGuard.Enter())
            {
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    screenList[i].screen.CompleteAnimation();
                }
            }
        }

        internal override void UpdateInteractable(ref bool interactable)
        {
            for (int i = screenList.Count - 1; i >= 0; i--)
            {
                IContainerItem item = screenList[i].screen;
                item.UpdateInteractable(ref interactable);
            }
        }

        internal override void UpdateOrder(ref int order)
        {
            for (int i = 0; i < screenList.Count; i++)
            {
                IContainerItem screen = screenList[i].screen;
                screen.UpdateOrder(ref order);
            }
        }

      
    }
}