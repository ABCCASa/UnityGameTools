using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameTools.UISystem
{
    public class ListContainer : ContainerBase
    {
        List<ScreenBase> screenList = new();
        public override int count => screenList.Count;
        public override TScreen Open<TScreen>(float fadeTime = -1) => Open<TScreen>(fadeTime, true);
        public override TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1) => Open<TScreen, TParam>(param, fadeTime, true);

        public TScreen Open<TScreen>(float fadeTime = -1, bool addAbove = true, ScreenBase relative = null) where TScreen : Screen
        {
            return OpenBase<TScreen>(screen => screen.SetOpen(fadeTime, this) ,addAbove, relative);
        }

        public TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1, bool addAbove = true, ScreenBase relative = null) where TScreen : Screen<TParam>
        {
            return OpenBase<TScreen>(screen => screen.SetOpen(param, fadeTime, this) ,addAbove, relative);
        }
        
        private TScreen OpenBase<TScreen>(Action<TScreen> openAction, bool addAbove, ScreenBase relative) where TScreen : ScreenBase
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            using (GetBusyScope())
            {
                if (relative != null)
                {
                    if (relative.state == ScreenState.Close) throw new Exception($"{relative} is already close, cannot use as relative screen");
                    if (!screenList.Contains(relative)) throw new Exception($"{relative} is not include in this Group");
                }
                var screen = UIManager.GetScreen<TScreen>();
                if (relative == null)
                {
                    if (addAbove) screenList.Add(screen);
                    else screenList.Insert(0, screen); 
                }
                else
                {
                    int index = screenList.IndexOf(relative);
                    screenList.Insert(addAbove ? index + 1 : index, screen);
                }
                openAction.Invoke(screen);
                UIManager.UpdateInteractable();
                UIManager.UpdateOrder();
                return screen;
            }
        }
    
        public void ChangeOrder(ScreenBase target, bool addAbove = true, ScreenBase relative = null)
        {
            using (GetBusyScope())
            {
                if(target==null) throw new  ArgumentNullException(nameof(target));
                if (target.state == ScreenState.Close) throw new Exception($"{target} is already close, cannot change order");
                if(!screenList.Contains(target)) throw new ArgumentException($"{target} is not include in this Group");
                if (relative != null)
                {
                    if (relative.state == ScreenState.Close) throw new Exception($"{relative} is already close, cannot use as relative screen");
                    if(!screenList.Contains(relative)) throw new ArgumentException($"{relative} is not include in this Group");
                }
                
                screenList.Remove(target);
                if (relative == null)
                {
                    if (addAbove) screenList.Add(target);
                    else screenList.Insert(0, target); 
                }
                else
                {
                    int index = screenList.IndexOf(relative);
                    screenList.Insert(addAbove ? index + 1 : index, target);
                }
                if (!isActive) return;
                UIManager.UpdateInteractable();
                UIManager.UpdateOrder();
            }
        }

        public override void Close(ScreenBase screen, float fadeTime = -1f)
        {
            using (GetBusyScope())
            {
                if (!screenList.Contains(screen)) throw new ArgumentException($"{screen} is not include in this Group");
                screen.SetClose(fadeTime, () =>
                {
                    screenList.Remove(screen);
                    UIManager.ReleaseScreen(screen);
                    UIManager.UpdateInteractable();
                });
            }
        }

        private protected override void OnResume()
        {
            using (GetBusyScope())
            {
                if (screenList.Count == 0) return;
                using (UIManager.GetDelayScope())
                {
                    foreach (ScreenBase screen in screenList) { screen.SetResume(); }
                    UIManager.UpdateOrder();
                    UIManager.UpdateInteractable();
                }
            }
        }

        private protected override void OnPause()
        {
            using (GetBusyScope())
            { 
                if (screenList.Count == 0) return;
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    ScreenBase screen = screenList[i];
                    if (screen.state == ScreenState.Close)
                    {
                        if (screen.isFade) screen.CompleteAnimation();
                        else throw new ArgumentException("List中出现完全关闭的Screen");
                    }
                    else screen.SetPause();
                }
                UIManager.UpdateInteractable();
            }
        }

        public override void CloseAll()
        {
            using (GetBusyScope())
            {
                if (screenList.Count == 0) return;
                for (int i = screenList.Count - 1; i >= 0; i--)
                {
                    var screen = screenList[i];
                    if (screen.state == ScreenState.Close)
                    {
                        if (screen.isFade) screen.CompleteAnimation(); // 回调中自带移除
                        else throw new ArgumentException("List中出现完全关闭的Screen");
                    }
                    else
                    { 
                        screenList.Remove(screen);
                        screen.SetClose(-1, () => { UIManager.ReleaseScreen(screen); });
                    }
                }
                Assert.IsTrue(screenList.Count  == 0);
                if (isActive) UIManager.UpdateInteractable();
            }
        }

        internal override void UpdateOrder(ref int order)
        {
            if (!isActive) return;
            foreach (IContainerItem screen in screenList)
            {
                screen.SetOrder(ref order);
            }
        }

        internal override void UpdateInteractable(ref bool interactable)
        {
            if (!isActive) return;
            for (int i = screenList.Count - 1; i >= 0; i--)
            {
                IContainerItem item = screenList[i];
                item.SetInteractable(ref interactable);
            }
        }
    }
}