using System;
using System.Collections.Generic;
using GameTools.Singletons;
using UnityEngine;

namespace GameTools.UISystem
{
    internal interface IAnimationHandler
    {
        public bool isComplete { get; }
        public void AddCallBack(Action onComplete);
        public void SpeedUpAnimation(float fadeTime);
        public void CompleteAnimation();
    }


    internal class UIAnimationManager : MonoLazySingleton<UIAnimationManager>
    {
        private class AnimationHandler : IAnimationHandler
        {
            private const float MinTime = 1 / 240f;
            public bool isComplete { get; private set; }
            private readonly bool forward;
            private float fadeTime;
            private float progress = 0f;
            private int lastUpdateFrame;
            private Action onComplete;
            private Action<float> animation;

            public AnimationHandler(float fadeTime, bool forward, Action<float> animation, Action onComplete)
            {
                animation = SafeCall(animation);
                if (onComplete != null) onComplete = SafeCall(onComplete);
                this.fadeTime = fadeTime;
                this.forward = forward;
                this.onComplete = onComplete;
                this.animation = animation;
                if (fadeTime <= MinTime)
                {
                    isComplete = true;
                    animation?.Invoke(forward ? 1 : 0);
                    onComplete?.Invoke();
                    return;
                }

                lastUpdateFrame = Time.frameCount;
                animation?.Invoke(forward ? 0 : 1);
            }

            public void AddCallBack(Action onComplete)
            {
                if (onComplete == null) return;
                onComplete = SafeCall(onComplete);
                if (isComplete) onComplete?.Invoke();
                else this.onComplete += onComplete;
            }

            public void SpeedUpAnimation(float fadeTime)
            {
                if (isComplete) return;
                if (fadeTime <= MinTime) CompleteAnimation();
                if (fadeTime < this.fadeTime) this.fadeTime = fadeTime;
            }

            public void CompleteAnimation()
            {
                if (isComplete) return;
                isComplete = true;
                animation?.Invoke(forward ? 1 : 0);
                onComplete?.Invoke();
            }

            public void Update(float deltaTime)
            {
                if (isComplete) return;
                if (lastUpdateFrame == Time.frameCount) return;

                progress += deltaTime / fadeTime;
                if (progress >= 1)
                {
                    isComplete = true;
                    animation?.Invoke(forward ? 1 : 0);
                    onComplete?.Invoke();
                }
                else
                {
                    lastUpdateFrame = Time.frameCount;
                    animation?.Invoke(forward ? progress : 1 - progress);
                }
            }

            private static Action SafeCall(Action action)
            {
                if (action == null) return null;
                return () =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                };
            }

            private static Action<float> SafeCall(Action<float> action)
            {
                if (action == null) return null;
                return (float value) =>
                {
                    try
                    {
                        action(value);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                };
            }
        }

        private const float MinTime = 1 / 240f;
        private readonly List<AnimationHandler> handlerList = new();
        public IAnimationHandler SetAnimation(float fadeTime, bool forward, Action<float> animation, Action onComplete = null)
        {
            var handler = new AnimationHandler(fadeTime, forward, animation, onComplete);
            if (!handler.isComplete) handlerList.Add(handler);
            return handler;
        }


        private void LateUpdate()
        {
            if (handlerList.Count == 0) return;
            using (LayerManager.GetDelayScope())
            {
                for (int i = handlerList.Count - 1; i >= 0; i--)
                {
                    var handler = handlerList[i];
                    if (handler.isComplete)
                    {
                        handlerList.RemoveAt(i);
                        continue;
                    }

                    handler.Update(Time.unscaledDeltaTime);
                    if (handler.isComplete)
                    {
                        handlerList.RemoveAt(i);
                    }
                }
            }
        }


        public Action SafeCall(Action action)
        {
            if (action == null) return null;
            return () =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };
        }

        public Action<float> SafeCall(Action<float> action)
        {
            if (action == null) return null;
            return (float value) =>
            {
                try
                {
                    action(value);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };
        }
    }
}