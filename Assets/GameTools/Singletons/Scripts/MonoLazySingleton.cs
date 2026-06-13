
using UnityEngine;
namespace GameTools.Singletons
{
    /// <summary>
    /// 以懒加载的方式在获取时自动创建一个Instance
    /// 通过AddComponent等方法创建时，不会被视为是单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MonoLazySingleton<T> : MonoBehaviour where T : MonoLazySingleton<T>
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new();
                    DontDestroyOnLoad(go);
                    go.name = typeof(T).Name;
                    _instance = go.AddComponent<T>();
                }
                return _instance;
            }
        }
    }
}