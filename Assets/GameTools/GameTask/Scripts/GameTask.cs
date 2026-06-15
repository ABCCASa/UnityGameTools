using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using UnityEngine.Pool;

namespace GameTools.GameTask
{
    public enum TaskStatus { InPool, Pending, Success, Fail }

    [AsyncMethodBuilder(typeof(GameTaskMethodBuilder))]
    public partial class GameTask : IAwaiter
    {
        private static readonly ObjectPool<GameTask> pool = new (createFunc: () => new GameTask(), collectionCheck: false);
        public bool IsCompleted => state == TaskStatus.Success || state == TaskStatus.Fail;
        private TaskStatus state = TaskStatus.InPool;
        private Exception exception;
        private Action continuation;
        private GameTask() { }
        internal static GameTask Get()
        {
            GameTask task = pool.Get();
            if (task.state != TaskStatus.InPool) { throw new InvalidOperationException("Object in pool not InPool"); }
            task.state = TaskStatus.Pending;
            return task;
        }

        private void Release()
        {
            if (!IsCompleted) { throw new InvalidOperationException("Cannot Return before completion"); }
            exception = null;
            continuation = null;
            state = TaskStatus.InPool;
            pool.Release(this);
        }

        private void OnCompleted(Action c)
        {
            if (continuation != null) { throw new InvalidOperationException("Only supports a single time await."); }
            continuation = c;
            if (IsCompleted) { continuation?.Invoke(); }
        }

        internal void SetResult()
        {
            if (IsCompleted) { throw new InvalidOperationException("Already completed"); }
            state = TaskStatus.Success;
            continuation?.Invoke();
            // 之后不要再执行任何代码了，_continuation内会完成GetResult等后续操作，在那时候会回收这个对象，所以后续的内容会修改对象池中的对象
        }

        internal void SetException(Exception ex)
        {
            if (IsCompleted) { throw new InvalidOperationException("Already completed"); }
            state = TaskStatus.Fail;
            exception = ex;
            continuation?.Invoke();
        }

        public IAwaiter GetAwaiter() => this;
        void IAwaiter.GetResult()
        {
            if (!IsCompleted) throw new InvalidOperationException("Result is not ready.");
            var e = exception;
            Release();
            if (e != null) { ExceptionDispatchInfo.Capture(e).Throw(); }
        }
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
        void INotifyCompletion.OnCompleted(Action continuation) => OnCompleted(continuation);
    }

    [AsyncMethodBuilder(typeof(GameTaskMethodBuilder<>))]
    public partial class GameTask<T> : IAwaiter<T>
    {
        private TaskStatus state = TaskStatus.InPool;
        public bool IsCompleted => state == TaskStatus.Success || state == TaskStatus.Fail;
        private Action continuation;
        private Exception exception;
        private T result;
        private static readonly ObjectPool<GameTask<T>> pool = new( createFunc: () => new GameTask<T>(), collectionCheck: false);

        private GameTask() { }
        internal static GameTask<T> Get()
        {
            GameTask<T> task = pool.Get();
            if (task.state != TaskStatus.InPool) { throw new InvalidOperationException("Object in pool not InPool"); }
            task.state = TaskStatus.Pending;
            return task;
        }
        private void Release()
        {
            if (!IsCompleted) { throw new InvalidOperationException("Cannot Return before completion"); }
            exception = null;
            continuation = null;
            result = default;
            state = TaskStatus.InPool;
            pool.Release(this);
        }

        internal void SetResult(T value)
        {
            if (IsCompleted) { throw new InvalidOperationException("Already completed"); }
            result = value;
            state = TaskStatus.Success;
            continuation?.Invoke();
        }

        internal void SetException(Exception ex)
        {
            if (IsCompleted) { throw new InvalidOperationException("Already completed"); }
            state = TaskStatus.Fail;
            exception = ex;
            continuation?.Invoke();
        }

        private void OnCompleted(Action c)
        {
            if (continuation != null) { throw new InvalidOperationException("Only supports a single time await."); }
            continuation = c;
            if (IsCompleted) { continuation?.Invoke(); }
        }

        public IAwaiter<T> GetAwaiter() => this;
        T IAwaiter<T>.GetResult()
        {
            if (!IsCompleted) throw new InvalidOperationException("Result is not ready.");
            var e = exception;
            var r = result;
            Release();
            if (e != null) { ExceptionDispatchInfo.Capture(e).Throw(); }
            return r;
        }
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
        void INotifyCompletion.OnCompleted(Action continuation) => OnCompleted(continuation);
    }
}
