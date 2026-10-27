using System;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace GameTools.GameTask
{
    public partial class GameTask
    {
        public static PlayerLoopAwaitable NextUpdate() => GameTaskLoopScheduler.update;
        public static PlayerLoopAwaitable NextFixedUpdate() => GameTaskLoopScheduler.fixedUpdate;
        public static PlayerLoopAwaitable NextLateUpdate() => GameTaskLoopScheduler.lateUpdate;
        
        public static async GameTask Delay(float seconds)
        {
            float endTime =  Time.time + seconds;
            while (Time.time < endTime)
            {
                await NextUpdate();
            }
        }

        public static async GameTask UnscaledDelay(float seconds)
        {
            float endTime =  Time.unscaledTime + seconds;
            while (Time.unscaledTime < endTime)
            {
                await NextUpdate();
            }
        }

        public static async GameTask WhenAll(params GameTask[] tasks)
        {
            if (tasks == null || tasks.Length == 0) return;
            Exception first = null;
            for (int i = 0; i < tasks.Length; i++)
            {
                try
                {
                    await tasks[i]; // 会触发 GetResult -> 回池
                }
                catch (Exception ex)
                {
                    first ??= ex;   // 先记下，但继续把后面的也 await 完，确保都回收
                }
            }
            if (first != null)  ExceptionDispatchInfo.Capture(first).Throw();;
        }
    }

    public partial class GameTask<T>
    {
        public static async GameTask<T[]> WhenAll(params GameTask<T>[] tasks)
        {
            if (tasks == null || tasks.Length == 0) return Array.Empty<T>();
            var results = new T[tasks.Length];
            Exception first = null;
            for (int i = 0; i < tasks.Length; i++)
            {
                try { results[i] = await tasks[i]; }
                catch (Exception ex)
                {
                    first ??= ex;
                }
            }
            if (first != null)  ExceptionDispatchInfo.Capture(first).Throw();
            return results;
        }
    }
}