using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTools.UISystem
{
    public class ListContainer : ContainerBase
    {
        List<ScreenBase> screenList = new();
        public bool isBusy { get; private set; } = false;

        public override T Open<T>(float fadeTime = -1)
        {
           return Open<T>(fadeTime, true);
        }

        public override TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1)
        {
            return Open<TScreen, TParam>(param, fadeTime, true);
        }

        public T Open<T>(float fadeTime = -1, bool addAbove = true) where T : Screen
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            isBusy = true;
            var screen = UIManager.GetScreen<T>();
            if (addAbove) screenList.Add(screen);
            else  screenList.Insert(0, screen);
            screen.SetOpen(fadeTime, this);
            UIManager.UpdateInteractable();
            UIManager.UpdateOrder();
            isBusy = false;
            return screen;
        }
 
        public TScreen Open<TScreen,TParam>(TParam param ,float fadeTime = -1, bool addAbove = true) where TScreen : Screen<TParam>
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            isBusy = true;
            var screen = UIManager.GetScreen<TScreen>();
            if (addAbove) screenList.Add(screen);
            else screenList.Insert(0, screen);
            screen.SetOpen(param, fadeTime, this);
            UIManager.UpdateInteractable();
            UIManager.UpdateOrder();
            isBusy = false;
            return screen;
        }

        public T Open<T>(ScreenBase relative, float fadeTime = -1, bool addAbove = true) where T : Screen
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            int index = screenList.IndexOf(relative);
            if(index<0) throw new Exception($"{relative} is not include in this Group");
            if (relative.state != ScreenState.Open) throw new Exception($"{relative} is not open");
            isBusy = true;
            var screen = UIManager.GetScreen<T>();
            screenList.Insert(addAbove ? index + 1 : index, screen);
            screen.SetOpen(fadeTime, this);
            UIManager.UpdateInteractable();
            UIManager.UpdateOrder();
            isBusy = false;
            return screen;
        } 
        
        public TScreen Open<TScreen, TParam>(TParam param,ScreenBase relative, float fadeTime = -1, bool addAbove = true) where TScreen : Screen<TParam>
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            int index = screenList.IndexOf(relative);
            if(index<0) throw new Exception($"{relative} is not include in this Group");
            if (relative.state != ScreenState.Open) throw new Exception($"{relative} is not open");
            isBusy = true;
            var screen = UIManager.GetScreen<TScreen>();
            screenList.Insert(addAbove ? index + 1 : index, screen);
            screen.SetOpen(param, fadeTime, this);
            UIManager.UpdateInteractable();
            UIManager.UpdateOrder();
            isBusy = false;
            return screen;
        }

        public override void Close(ScreenBase screen, float fadeTime = -1f)
        {
            if (isBusy) throw new Exception("不要在screen生命周期(OnOpen, OnClose...)内修改自身所属的LayerGroup的状态");
            isBusy = true;
            if (!screenList.Contains(screen)) throw new ArgumentException($"{screen} is not include in this Group");
            screen.SetClose(fadeTime, () =>
            {
                screenList.Remove(screen);
                UIManager.ReleaseScreen(screen);
                UIManager.UpdateInteractable();
            });
            isBusy = false;
        }

        private protected override void OnResume()
        {
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            if(screenList.Count == 0) return;
            UIManager.DelayStateUpdate(() =>
            {
                isBusy = true;
                foreach (ScreenBase screen in screenList)
                {
                    screen.SetResume();
                    UIManager.UpdateOrder();
                    UIManager.UpdateInteractable();
                }
                isBusy = false;
            });
        }

        private protected override void OnPause()
        {
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            if(screenList.Count == 0) return;
            isBusy = true;
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
            isBusy = false;
        }

        public override void CloseAll()
        {
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            if(screenList.Count == 0) return; 
            isBusy = true;
            for (int i =  screenList.Count - 1; i >= 0; i--)
            {  
                var screen = screenList[i];
                if (screen.state == ScreenState.Close)
                {
                    if (screen.isFade) screen.CompleteAnimation(); // 回调中自带移除
                    else throw new ArgumentException("List中出现完全关闭的Screen");
                }
                else
                {
                    screen.SetClose(-1, () => { UIManager.ReleaseScreen(screen); });
                    screenList.Remove(screen);
                }
            }
            if (screenList.Count != 0) Debug.LogError("出现意外，Clear未完成清除任务");
            if(isActive) UIManager.UpdateInteractable();
            isBusy = false;
        }

        internal override void UpdateOrder(ref int order)
        {
            if (!isActive) return;
            foreach (ILayerItem screen in screenList)
            {
                screen.SetOrder(order);
                order++;
            }
        }

        internal override void UpdateInteractable(ref bool interactable)
        {
            if (!isActive) return;
            for (int i = screenList.Count - 1; i >= 0; i--)
            {
                ScreenBase screen = screenList[i];
                if (screen.state != ScreenState.Open && !screen.isFade) continue;  // 忽略完全隐藏的Screen
                ILayerItem item = screen;
                item.SetInteractable(interactable);
                if ( item.blockInput) interactable = false;
            }
        }
    }
}