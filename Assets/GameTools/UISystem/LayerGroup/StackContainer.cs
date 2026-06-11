using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


namespace GameTools.UISystem
{
    public class StackContainer : ContainerBase
    {
        private List<ScreenBase> screenStack = new();
        public bool isBusy { get; private set; } = false;
        public override T Open<T>(float fadeTime = -1) => Push<T>(fadeTime);
        public override TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1) => Push<TScreen, TParam>(param, fadeTime);
        private ScreenBase Peek(ScreenState state) => screenStack.FindLast(s => s.state == state);

        public T Push<T>(float fadeTime) where T : Screen
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            isBusy = true;
            ScreenBase pauseScreen = Peek(ScreenState.Open);
            pauseScreen?.SetPause(fadeTime, UIManager.UpdateInteractable);
            var screen = UIManager.GetScreen<T>();
            screenStack.Add(screen);
            screen.SetOpen(fadeTime, this);
            UIManager.UpdateOrder();
            UIManager.UpdateInteractable();
            isBusy = false;
            return screen;
        }

        public TScreen Push<TScreen, TParam>(TParam param, float fadeTime) where TScreen : Screen<TParam>
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            isBusy = true;
            ScreenBase pauseScreen = Peek(ScreenState.Open);
            pauseScreen?.SetPause(fadeTime, UIManager.UpdateInteractable);
            var screen = UIManager.GetScreen<TScreen>();
            screenStack.Add(screen);
            screen.SetOpen(param, fadeTime, this);
            UIManager.UpdateOrder();
            UIManager.UpdateInteractable();
            isBusy = false;
            return screen;
        }

        public void Pop(float fadeTime)
        {
            if (!isActive) throw new Exception("未active的情况下无法Pop");
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            isBusy = true;
            ScreenBase closeScreen = Peek(ScreenState.Open);
            if (closeScreen == null) return;
            closeScreen.SetClose(fadeTime, () =>
            {
                screenStack.Remove(closeScreen);
                UIManager.ReleaseScreen(closeScreen);
                UIManager.UpdateInteractable();
            });
            ScreenBase resumeScreen = Peek(ScreenState.Pause);
            if (resumeScreen == null) return;
            resumeScreen.SetResume(fadeTime);
            UIManager.UpdateInteractable();
            isBusy = false;
        }
        public override void Close(ScreenBase screen, float fadeTime = -1)
        {
            if (!screenStack.Contains(screen)) throw new Exception($"{screen} is not include in this Group");
            if (screen.state == ScreenState.Open)
            {
                Assert.IsTrue(screen==Peek(ScreenState.Open), "出现错误，screenStack出现两个打开的screen"); // 进一步确认pop移除的就是指定的screen
                Pop(fadeTime);
            }
            else // 移除暂停状态的Screen
            {
                if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
                isBusy = true;
                Debug.LogWarning($"正在关闭非顶层打开的 Screen: {screen}");
                screen.SetClose(-1, () => { UIManager.ReleaseScreen(screen); });
                screenStack.Remove(screen);
                UIManager.UpdateInteractable();
                isBusy = false;
            }
        }

        private protected override void OnResume()
        {
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            if (screenStack.Count == 0) return;
            isBusy = true;
            ScreenBase screen = Peek(ScreenState.Pause);
            if (screen == null) return;
            screen.SetResume();
            UIManager.UpdateOrder();
            UIManager.UpdateInteractable();
            isBusy = false;
        }

        private protected override void OnPause()
        {
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            if (screenStack.Count == 0) return;
            using (UIManager.DelayStateUpdateScope())
            {
                isBusy = true;
                // 清理退出一半的Screen
                for (int i = screenStack.Count - 1; i >= 0; i--)
                {
                    var screen = screenStack[i];
                    if (screen.state != ScreenState.Close) continue;
                    if (screen.isFade) screen.CompleteAnimation();
                    else throw new ArgumentException("List中出现完全关闭的Screen"); // 额外的保护，只要内部逻辑正常就不会触发
                }
                Peek(ScreenState.Open)?.SetPause();
                UIManager.UpdateOrder();
                UIManager.UpdateInteractable();
                isBusy = false;
            }
        }

        public override void CloseAll()
        {
            if (isBusy) throw new Exception("不要在screen生命周期内修改自身所属的LayerGroup的状态");
            if (screenStack.Count == 0) return;
            using (UIManager.DelayStateUpdateScope())
            {
                isBusy = true;
                for (int i = screenStack.Count - 1; i >= 0; i--)
                {  
                    var screen =screenStack[i];
                    if (screen.state == ScreenState.Close)
                    {
                        if (screen.isFade) screen.CompleteAnimation(); // 回调中自带移除
                        else throw new ArgumentException("List中出现完全关闭的Screen");
                    }
                    else
                    {
                        screen.SetClose(-1, () => { UIManager.ReleaseScreen(screen); });
                        screenStack.Remove(screen);
                    }
                }
                if (screenStack.Count != 0) Debug.LogError("出现意外，Clear未完成清除任务");
                if (isActive) UIManager.UpdateInteractable();
                isBusy = false;
            }
        }

        internal override void UpdateOrder(ref int order)
        {
            if (!isActive) return;
            foreach (ILayerItem screen in screenStack)
            {
                screen.SetOrder(order);
                order++;
            }
        }

        internal override void UpdateInteractable(ref bool interactable)
        {
            if (!isActive) return;
            for (int i = screenStack.Count - 1; i >= 0; i--)
            {
                ScreenBase screen = screenStack[i];
                if (screen.state != ScreenState.Open && !screen.isFade) continue;
                ILayerItem item = screen;
                item.SetInteractable(interactable);
                if (item.blockInput) interactable = false;
            }
        }
    }
}