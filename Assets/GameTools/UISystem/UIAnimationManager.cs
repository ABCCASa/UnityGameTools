using System;
using System.Collections.Generic;
using GameTools.Singletons;
using UnityEngine;

namespace GameTools.UISystem
{
    internal class UIAnimationManager : MonoLazySingleton<UIAnimationManager>
    {
        private const float MinTime = 1 / 240f;
        private readonly List<AnimController> controllers = new();
        private struct AnimController
        {
            public bool isComplete;
            public int lastUpdateFrame;
            public readonly object reference;
            public readonly bool forward;
            public float progress;
            public float speed;
            public readonly Action<float> animation;
            public readonly Action onComplete;
            public AnimController(object reference, float speed, bool forward, Action<float> animation, Action onComplete, int lastUpdateFrame)
            {
                progress = 0;
                this.reference = reference;
                this.speed = speed;
                this.forward = forward;
                this.animation = animation;
                this.onComplete = onComplete;
                this.lastUpdateFrame = lastUpdateFrame;
                isComplete = false;
            }
        }

        public void SetAnimation(object reference, float fadeTime, bool forward, Action<float> animation, Action onComplete)
        {
            animation = SafeCall(animation);
            onComplete = SafeCall(onComplete);
            CompleteAnimation(reference);
            if (fadeTime <= MinTime)
            {
                animation?.Invoke(forward ? 1 : 0);
                onComplete?.Invoke();
                return;
            }
            animation?.Invoke(forward ? 0 : 1);
            AnimController controller = new AnimController(reference, 1 / fadeTime, forward, animation, onComplete, Time.frameCount);
            controllers.Add(controller);
        }

        private int IndexOf(object reference) => controllers.FindIndex(x => x.reference == reference && !x.isComplete);
        
        public bool HasAnimation(object reference)
        {
            int index = IndexOf(reference);
            return index >= 0;
        }

        public bool SpeedUpAnimation(object reference, float fadeTime)
        {
            if (fadeTime <= MinTime) return CompleteAnimation(reference);
            int index = IndexOf(reference);
            if (index == -1) return false;
            var controller = controllers[index];
            controller.speed = Mathf.Max(1 / fadeTime, controller.speed);
            controllers[index] = controller;
            return true;
        }

        public bool CompleteAnimation(object reference)
        {
            int index = IndexOf(reference);
            if (index == -1) return false;
            var controller = controllers[index];
            controller.isComplete = true;
            controllers[index] = controller;
            controller.animation?.Invoke(controller.forward ? 1 : 0);
            controller.onComplete?.Invoke();
            return true;
        }

        private void LateUpdate()
        {
            if(controllers.Count == 0) return;
            using (UIManager.GetDelayScope())
            {
                int currentFrame = Time.frameCount;
                for (int i = controllers.Count - 1; i >= 0; i--)
                {
                    var controller = controllers[i];
                    if (controller.isComplete)
                    {
                        controllers.RemoveAt(i);
                        continue;
                    }
                    if (controller.lastUpdateFrame >= currentFrame) continue;
                    controller.progress += Time.unscaledDeltaTime * controller.speed;
                    if (controller.progress >= 1)
                    {
                        controllers.RemoveAt(i);
                        controller.animation?.Invoke(controller.forward ? 1 : 0);
                        controller.onComplete?.Invoke();
                    }
                    else
                    {
                        controller.lastUpdateFrame = currentFrame;
                        controllers[i] = controller;
                        float p = controller.progress;
                        controller.animation?.Invoke(controller.forward ? p : 1 - p);
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