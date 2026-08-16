using System;
using System.Collections.Generic;
using GameTools.Singletons;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace GameTools.UISystem
{
    public class ResourcesScreenLoader: MonoLazySingleton<ResourcesScreenLoader>, IScreenLoader
    {
        private readonly Dictionary<Type, DynamicObjectPool<ScreenBase>> screenPools = new(64);
        private DynamicObjectPool<ScreenBase> GetPool(Type type)
        {
            if (screenPools.TryGetValue(type, out var pool)) return pool;
            string path = $"UI/Screens/{type.Name}";;
            pool = new DynamicObjectPool<ScreenBase> (
                createFunc: () =>
                {
                    ScreenBase original = Resources.Load<ScreenBase>(path);
                    if (original == null) throw new Exception($"Cannot find the ui based in path({path})");
                    if(original.GetType() != type) throw new Exception($"{type} is a base type of the instance, not its concrete runtime type. Actual type: {original.GetType()}.");
                    ScreenBase ui = Object.Instantiate(original, transform);
                    ui.gameObject.name = original.gameObject.name;
                    ui.SetInit();
                    return ui;
                },
                actionOnDestroy: (ui) =>
                {
                    ui.SetDispose();
                },
                collectionCheck: true, retainedCount: 1, baseReleaseDelay: 20, releaseDelayDecay: 0.9f  
            );
            screenPools[type] = pool;
            return pool;
        }
        
        public T GetScreen<T>() where T : ScreenBase
        {
            var pool = GetPool(typeof(T));
            T ui = (T)pool.Get();
            return ui;
        }
        public void ReleaseScreen(ScreenBase screen)
        {
            if (screen == null) throw new Exception("screen为null");
            var pool = GetPool(screen.GetType());
            pool.Release(screen);  
        }

        public void ClearPools()
        {
            foreach (var pool in screenPools.Values)
            {
                pool.Clear();
            }
        }
        
        private float deltaTime = 0;
        public void LateUpdate()
        {
            deltaTime += Time.unscaledDeltaTime;
            if (Time.frameCount % 30 != 0) return; // 减少更新次数，大概30秒更新一次
            foreach (var pool in screenPools.Values)
            {
                pool.Update(deltaTime);
            }
            deltaTime = 0;
        }
    }
}