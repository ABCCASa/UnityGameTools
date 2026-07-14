using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameTools.UISystem
{
    public class SwapContainer : ContainerBase
    {
        private ScreenBase currentScreen;
        private readonly List<ScreenBase> fadeOutScreens = new();
        public override int count => fadeOutScreens.Count + (currentScreen == null ? 0 : 1);
        public override TScreen Open<TScreen>(float fadeTime = -1) 
        {
            return OpenBase<TScreen>(screen => screen.SetOpen(fadeTime, this),fadeTime);
        }

        public override TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1) 
        {
          return OpenBase<TScreen>(screen => screen.SetOpen(param, fadeTime, this),fadeTime);
        }

        private TScreen OpenBase<TScreen>(Action<TScreen> openAction, float fadeTime) where TScreen : ScreenBase
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            using (GetBusyScope())
            {
                if (currentScreen != null)
                {
                    ScreenBase closeScreen = currentScreen;
                    currentScreen = null;
                    fadeOutScreens.Add(closeScreen);
                    closeScreen?.SetClose(fadeTime, () =>
                    {
                        fadeOutScreens.Remove(closeScreen);
                        UIManager.ReleaseScreen(closeScreen);
                        UIManager.UpdateInteractable();
                    });
                }
                var screen = UIManager.GetScreen<TScreen>();
                openAction.Invoke(screen);
                currentScreen = screen;
                UIManager.UpdateOrder();
                UIManager.UpdateInteractable();
                return screen;
            }
        }


        public override void Close(ScreenBase screen, float fadeTime = -1)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));
            if (currentScreen != screen) throw new Exception($"{screen} is not include in this Group");
            using (GetBusyScope())
            {
                ScreenBase closeScreen = currentScreen;
                currentScreen = null;
                fadeOutScreens.Add(closeScreen);
                closeScreen?.SetClose(fadeTime, () =>
                {
                    fadeOutScreens.Remove(closeScreen);
                    UIManager.ReleaseScreen(closeScreen);
                    UIManager.UpdateInteractable();
                });
            }
        }

        public void SpeedUpAnimations(float fadeTime)
        {
            for (int i = fadeOutScreens.Count - 1; i >= 0; i--)
            {
                var screen =fadeOutScreens[i];
                if (screen.isFade) screen.SpeedUpAnimation(fadeTime);
            }
            if(currentScreen != null && currentScreen.isFade) currentScreen.SpeedUpAnimation(fadeTime);
        }

        public void CompleteAnimations()
        {
            for (int i = fadeOutScreens.Count - 1; i >= 0; i--)
            {
                var screen = fadeOutScreens[i];
                if (screen.isFade) screen.CompleteAnimation();
            }
            Assert.IsTrue(fadeOutScreens.Count == 0);
            if(currentScreen != null && currentScreen.isFade) currentScreen.CompleteAnimation();
        }

        private protected override void OnPause()
        {
            using (GetBusyScope())
            {
                using (UIManager.GetDelayScope())
                {
                    if(currentScreen != null)  currentScreen.SetPause();
                    CompleteAnimations(); // 清理退出一半的Screen
                    UIManager.UpdateOrder();
                    UIManager.UpdateInteractable();
                }
            }
        }

        private protected override void OnResume()
        {
            using (GetBusyScope())
            {
                if (currentScreen == null) return;
                currentScreen.SetResume();
                UIManager.UpdateOrder();
                UIManager.UpdateInteractable();
            }
        }

        public override void CloseAll()
        {
            using (GetBusyScope())
            {
                using (UIManager.GetDelayScope())
                {
                    if (currentScreen != null)
                    {
                        var screen = currentScreen;
                        currentScreen = null;
                        screen.SetClose(-1, () => { UIManager.ReleaseScreen(screen); });
                    }
                    CompleteAnimations();
                    if (isActive) UIManager.UpdateInteractable();
                }
            }
        }

        internal override void UpdateOrder(ref int order)
        {
            if (!isActive) return;
            foreach (ScreenBase screen in fadeOutScreens)
            {
                ProcessScreen(screen, ref order);
            }
            if (currentScreen != null) ProcessScreen(currentScreen, ref order);
            return;
            void ProcessScreen(IContainerItem screen, ref int order)
            {
                screen.SetOrder(ref order);
            }
        }

        internal override void UpdateInteractable(ref bool interactable)
        {
            if (!isActive) return;
            if (currentScreen != null) ProcessScreen(currentScreen, ref interactable);
            for (int i = fadeOutScreens.Count - 1; i >= 0; i--)
            {
                ProcessScreen(fadeOutScreens[i], ref interactable);
            }
            return;
            void ProcessScreen(IContainerItem item, ref bool interactable)
            {
                item.SetInteractable(ref interactable);
            }
        }
    }
}