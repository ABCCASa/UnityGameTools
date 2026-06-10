using UnityEngine;
namespace GameTools.Singletons
{
    /// <summary> 不会被自动创建，需要通过AddComponent等方式手动创建，之后可通过Instance获取
    /// 场景中只存在一个，多余的会被销毁，基于定义的销毁规则</summary>
    /// <typeparam name="T"></typeparam>
    [DisallowMultipleComponent]
    public abstract class MonoManualSingleton<T> : MonoBehaviour where T : MonoManualSingleton<T>
    {
        /// <summary>是否允许被替换，以当前正在使用的单例为准</summary>
        protected abstract bool allowReplace { get; }

        /// <summary>在被替换或被拒绝替换时，要如何销毁</summary>
        protected abstract bool onlyDestroyClass { get; }

        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null) { _instance = null; }  // m_instance 销毁后，比较null为true，但依然存在所以要清空
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }
        protected virtual void Awake()
        {
            if (Instance != null)
            {
                if (Instance.allowReplace != allowReplace)
                {
                    Debug.LogWarning($" {typeof(T).FullName} 单例类，的替换规则未达成统一，以当前正在使用的单例为准");
                }

                if (Instance.allowReplace)
                {
                    Debug.Log($"发现重复的 {typeof(T).FullName} 单例类，按照替换规则，将单例更新为新生成的");
                    Instance.DestroySingleton();
                }
                else
                {
                    Debug.LogWarning($"发现重复的 {typeof(T).FullName} 单例类，按照替换规则，只保留原始的单例");
                    DestroySingleton();
                    return;
                }
            }
            Instance = this as T;
        }

        private void DestroySingleton()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (onlyDestroyClass)
            {
                Destroy(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

    }
}