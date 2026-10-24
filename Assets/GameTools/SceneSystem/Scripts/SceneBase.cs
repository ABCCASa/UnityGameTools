using GameTools.Singletons;
using System;
using System.Collections;

namespace GameTools.SceneSystem
{
    public abstract class SceneBase : MonoManualSingleton<SceneBase>
    {
        protected sealed override bool allowReplace => true;
        protected sealed override bool onlyDestroyClass => false;
        internal IEnumerator OpenScene<T>(T param)
        {
            SceneBase<T> scene = this as SceneBase<T>;
            if (scene == null) { throw new Exception($"{typeof(T).Name} 无法用于 {GetType().Name}的打开");  }
            return scene.OpenScene(param);
        }

        internal IEnumerator OpenScene() => OnOpen();
        internal IEnumerator CloseScene() => OnClose();
 
        /// <summary> 当无参数打开场景时调用，有参数打开场景请实现IOpenScene接口 </summary>
        protected virtual IEnumerator OnOpen() { yield break; }
        protected virtual IEnumerator OnClose() { yield break; }
    }

    public class SceneBase<T> : SceneBase
    {
        internal IEnumerator OpenScene(T param) => OnOpen(param);
        protected override IEnumerator OnOpen() { throw new Exception($"{GetType().Name} 默认不实现无参数打开场景，请实现OnOpen()方法"); }
        protected virtual IEnumerator OnOpen(T param) { yield break; }
    }
}

