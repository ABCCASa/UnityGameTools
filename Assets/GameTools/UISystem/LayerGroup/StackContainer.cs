using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameTools.UISystem
{
    public class StackContainer : ContainerBase
    {
        private List<ScreenBase> screenStack = new();
        public override int count => screenStack.Count;
        public override T Open<T>(float fadeTime = -1) => Push<T>(fadeTime);
        public override TScreen Open<TScreen, TParam>(TParam param, float fadeTime = -1) => Push<TScreen, TParam>(param, fadeTime);
        private ScreenBase Peek(ScreenState state) => screenStack.FindLast(s => s.state == state);

        public TScreen Push<TScreen>(float fadeTime = -1) where TScreen : Screen
        {
            return PushBase<TScreen>(screen => screen.SetOpen(fadeTime, this,"open"), fadeTime);
        }

        public TScreen Push<TScreen, TParam>(TParam param, float fadeTime = -1) where TScreen : Screen<TParam>
        {
           return PushBase<TScreen>(screen => screen.SetOpen(param, fadeTime, this,"open"), fadeTime);
        }

        private TScreen PushBase<TScreen>(Action<TScreen> onOpen,float fadeTime = -1) where TScreen : ScreenBase
        {
            if (!isActive) throw new Exception("未active的情况下无法添加");
            using (GetBusyScope())
            {
                ScreenBase pauseScreen = Peek(ScreenState.Open);
                pauseScreen?.SetPause(fadeTime, "pause", UIManager.UpdateInteractable);
                var screen = UIManager.GetScreen<TScreen>();
                screenStack.Add(screen);
                onOpen.Invoke(screen);
                UIManager.UpdateOrder();
                UIManager.UpdateInteractable();
                return screen;
            }
        }

        public void Pop(float fadeTime)
        {
            if (!isActive) throw new Exception("未active的情况下无法Pop");
            using (GetBusyScope())
            {
                using (UIManager.GetDelayScope())
                {
                    ScreenBase closeScreen = Peek(ScreenState.Open);
                    if (closeScreen == null) return;
                    closeScreen.SetClose(fadeTime, "close", () =>
                    {
                        screenStack.Remove(closeScreen);
                        UIManager.ReleaseScreen(closeScreen);
                        UIManager.UpdateInteractable();
                    });
                    ScreenBase resumeScreen = Peek(ScreenState.Pause);
                    if (resumeScreen == null) return;
                    resumeScreen.SetResume(fadeTime, "resume");
                    UIManager.UpdateInteractable();
                }
            }
        }

        public override void Close(ScreenBase screen, float fadeTime = -1)
        {
            if (!screenStack.Contains(screen)) throw new Exception($"{screen} is not include in this Group");
            if (screen.state == ScreenState.Open)
            {
                Assert.IsTrue(screen == Peek(ScreenState.Open), "出现错误，screenStack出现两个打开的screen"); // 确认pop移除的就是指定的screen
                Pop(fadeTime);
            }
            else // 移除暂停状态的Screen
            {
                using (GetBusyScope())
                {
                    Debug.LogWarning($"正在关闭非顶层打开的 Screen: {screen}");
                    screen.SetClose(-1, "close", () => { UIManager.ReleaseScreen(screen); });
                    screenStack.Remove(screen);
                    UIManager.UpdateInteractable();
                }
            }
        }

        public void SpeedUpAnimations(float fadeTime)
        {
            for (int i = screenStack.Count - 1; i >= 0; i--)
            {
                var screen = screenStack[i];
                if (screen.isFade) screen.SpeedUpAnimation(fadeTime);
                else Assert.IsFalse(screen.state == ScreenState.Close);
            }
        }

        public void CompleteAnimations()
        {
            for (int i = screenStack.Count - 1; i >= 0; i--)
            {
                var screen = screenStack[i];
                if (screen.isFade) screen.CompleteAnimation();
                else Assert.IsFalse(screen.state == ScreenState.Close);
            }
        }

        private protected override void OnPause()
        {
            using (GetBusyScope())
            {
                if (screenStack.Count == 0) return;
                using (UIManager.GetDelayScope())
                {
                    Peek(ScreenState.Open)?.SetPause(animKey: "pauseAll");
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
                if (screenStack.Count == 0) return;
                ScreenBase screen = Peek(ScreenState.Pause);
                if (screen == null) return;
                screen.SetResume(animKey: "resumeAll");
                UIManager.UpdateOrder();
                UIManager.UpdateInteractable();
            }
        }

        public override void CloseAll(float fadeTime = -1)
        {
            using (GetBusyScope())
            {
                if (screenStack.Count == 0) return;
                using (UIManager.GetDelayScope())
                {
                    for (int i = screenStack.Count - 1; i >= 0; i--)
                    {
                        var screen = screenStack[i];
                        if (screen.state == ScreenState.Close)
                        {
                            Assert.IsTrue(screen.isFade);
                            screen.SpeedUpAnimation(fadeTime); // 加快动画
                        }
                        else
                        {
                            screen.SetClose(fadeTime, "closeAll",() =>
                            {
                                screenStack.Remove(screen);
                                UIManager.ReleaseScreen(screen);
                                UIManager.UpdateInteractable();
                            });
                        }
                    }
                }
            }
        }

        internal override void UpdateOrder(ref int order)
        {
            if (!isActive) return;
            foreach (IContainerItem screen in screenStack)
            {
                screen.SetOrder(ref order);
            }
        }

        internal override void UpdateInteractable(ref bool interactable)
        {
            if (!isActive) return;
            for (int i = screenStack.Count - 1; i >= 0; i--)
            {
                IContainerItem item = screenStack[i];
                item.SetInteractable(ref interactable);
            }
        }
    }
}