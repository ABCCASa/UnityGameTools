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
            private const float MinTime = 1 / 120f;
            public bool isComplete { get; private set; }
            private readonly bool forward;
            private float fadeTime;
            private float progress;
            private int lastUpdateFrame;
            private readonly Action<float> animation;
            private Action onComplete;

            private AnimationHandler(float fadeTime, bool forward, Action<float> animation, Action onComplete)
            {
                isComplete = false;
                this.forward = forward;
                this.fadeTime = fadeTime;
                progress = 0;
                this.animation = animation;
                this.onComplete = onComplete;
                animation?.Invoke(forward ? 0 : 1);
                lastUpdateFrame = Time.frameCount; 
            }

            public static AnimationHandler Create(float fadeTime, bool forward, Action<float> animation, Action onComplete)
            {
                onComplete = SafeCall(onComplete);
                animation = SafeCall(animation);
                if (fadeTime > MinTime) return new AnimationHandler(fadeTime, forward, animation, onComplete);
                animation?.Invoke(forward ? 1 : 0);
                onComplete?.Invoke();
                return null;
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
                if (fadeTime <= MinTime)
                {
                    CompleteAnimation();
                    return;
                }
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
                return (value) =>
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

        private readonly List<AnimationHandler> handlerList = new();
        public IAnimationHandler RegisterAnimation(float fadeTime, bool forward, Action<float> animation, Action onComplete = null)
        {
            var handler = AnimationHandler.Create(fadeTime, forward, animation, onComplete);
            if (handler != null)  handlerList.Add(handler);  
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
    }
}