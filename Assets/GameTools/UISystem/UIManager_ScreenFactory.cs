using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Object = UnityEngine.Object;


namespace GameTools.UISystem
{
    public static partial class UIManager
    {
        private static Transform _root;
        private static Transform root
        {
            get
            {
                if (_root == null)
                {
                    GameObject obj = new GameObject("UI Manager");
                    Object.DontDestroyOnLoad(obj);
                    _root = obj.transform;
                }
                return _root;
            }
        }

        private static readonly int maxCacheCount = 2;

        private static readonly Dictionary<Type, ObjectPool<ScreenBase>> screenPools = new(64);

        private static ObjectPool<ScreenBase> GetPool(Type type)
        {
            if (!screenPools.TryGetValue(type, out ObjectPool<ScreenBase> pool))
            {
                pool = new ObjectPool<ScreenBase>(
                    createFunc: () =>
                    {
                        string path = $"UI/Screens/{type.Name}";
                        ScreenBase original = Resources.Load<ScreenBase>(path);
                        if (original == null) { throw new Exception($"Cannot find the ui based in path({path})"); }
                        ScreenBase ui = Object.Instantiate(original, root);
                        ui.gameObject.name = original.gameObject.name;
                        ui.SetInit();
                        return ui;
                    },
                    actionOnDestroy: (ui) =>
                    {
                        ui.SetDispose();
                    },
                    collectionCheck: true, defaultCapacity: maxCacheCount, maxSize: maxCacheCount
                );
                screenPools[type] = pool;
            }
            return pool;
        }

        // 创建后者从对象池中获得Screen
        internal static T GetScreen<T>() where T : ScreenBase
        {
            var pool = GetPool(typeof(T));
            T ui = (T)pool.Get();
            return ui;
        } 
        
        /// <summary> 注销已经关闭的UI</summary>
        internal static void ReleaseScreen(ScreenBase screen)
        {
            if (screen == null) throw new Exception("screen为null");
            var pool = GetPool(screen.GetType());
            pool.Release(screen);            
        }
        
        
        
    }
}