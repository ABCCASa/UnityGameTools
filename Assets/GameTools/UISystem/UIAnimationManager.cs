using System;
using System.Collections.Generic;
using GameTools.Singletons;
using UnityEngine;

namespace GameTools.UISystem
{
    public class UIAnimationManager: MonoLazySingleton<UIAnimationManager>
    {
        public const float minTime = 1 / 60f;
        List<UIAnimationTask> controllers = new();
        public struct UIAnimationTask
        {
            public readonly object reference;
            public readonly bool forward;
            public float progress;
            public float speed;
            public readonly Action<float> animation;
            public readonly Action onComplete;

            public UIAnimationTask(object reference, float speed, bool forward, Action<float> animation, Action onComplete)
            {
                progress = 0;
                this.reference = reference;
                this.speed = speed;
                this.forward = forward;
                this.animation = animation;
                this.onComplete = onComplete;
            }
        }

        public void SetAnimation(object reference, float fadeTime, bool forward, Action<float> animation, Action onComplete)
        {
            CompleteAnimation(reference);
            if (fadeTime <= minTime)
            {  
                animation?.Invoke(forward?1:0);
                onComplete?.Invoke();
                return;
            }
            animation?.Invoke(forward?0:1);
            UIAnimationTask controller = new (reference, 1 / fadeTime, forward, animation, onComplete);
            controllers.Add(controller);
        }


        public  bool HasAnimation(object reference)
        {
            int index = controllers.FindIndex(x => x.reference == reference);
            return index >= 0;
        }

        public void SpeedUpAnimation(object reference, float fadeTime)
        {
            int index = controllers.FindIndex(x => x.reference == reference);
            if (index != -1)
            {
                var controller = controllers[index];
                if (fadeTime <= minTime)
                {
                    controllers.RemoveAt(index);
                    controller.animation?.Invoke(controller.forward?1:0);
                    controller.onComplete?.Invoke();
                }
                else
                {
                    controller.speed += 1 / fadeTime;
                    controllers[index] = controller;
                }
            }  
        }

        public void CompleteAnimation(object reference)
        {
            int index = controllers.FindIndex(x => x.reference == reference);
            if (index != -1)
            {   
                var controller = controllers[index];
                controllers.RemoveAt(index);
                controller.animation?.Invoke(controller.forward?1:0);
                controller.onComplete?.Invoke();
            }  
        }

        private void Update()
        {
            using ( UIManager.DelayStateUpdateScope())
            {
                for (int i = controllers.Count - 1; i >= 0; i--)
                {
                    var controller = controllers[i];
                    controller.progress += Time.unscaledDeltaTime * controller.speed;
                    if (controller.progress >= 1)
                    { 
                        controllers.RemoveAt(i);
                        controller.animation?.Invoke(controller.forward?1:0);
                        controller.onComplete?.Invoke();
                    }
                    else
                    {
                        controllers[i] = controller;
                        float p = controller.progress;
                        controller.animation?.Invoke(controller.forward? p : 1-p);
                    }
                }
            }
        }
    }
}