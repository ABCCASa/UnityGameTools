using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;


namespace GameTools.GameTask
{
    internal interface IStateMachineRunner
    {
        Action Continuation { get; }
        void Release();
    }

    internal sealed class StateMachineBox<TStateMachine> : IStateMachineRunner where TStateMachine : IAsyncStateMachine
    {
        private static readonly ObjectPool<StateMachineBox<TStateMachine>> pool = new(() => new StateMachineBox<TStateMachine>(), collectionCheck: false);
        private TStateMachine stateMachine;
        private readonly Action _continuation;
        public Action Continuation => _continuation;

        private StateMachineBox()
        {
            _continuation = MoveNext;
        }

        public static StateMachineBox<TStateMachine> Get()
        {
            return pool.Get();
        }

        public void Init(ref TStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        private void MoveNext()
        {
            stateMachine.MoveNext();
        }

        public void Release()
        {
            stateMachine = default;
            pool.Release(this);
        }
    }

    public struct GameTaskMethodBuilder
    {
        private IStateMachineRunner smBox;
        private GameTask task;
        public GameTask Task => task;

        public static GameTaskMethodBuilder Create() => new GameTaskMethodBuilder();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            task = GameTask.Get();
            var box = StateMachineBox<TStateMachine>.Get();
            smBox = box; //先把 box 赋给 builder 的 sm，是因为状态机里包含 builder；Init 复制状态机时，只有这样复制进去的那份状态机副本里的 builder 才会带着 sm = box，从而在后续 await 挂起时，能够通过 builder 找回这个 box。
            box.Init(ref stateMachine);
            smBox.Continuation();
        }

        public void SetResult()
        {
            var t = task; // 提前缓存source，否则 sm_box 被修改时里面是sm会被初始化，导致source丢失
            smBox.Release();
            t.SetResult();
        }

        public void SetException(Exception ex)
        {
            var t = task;
            smBox.Release();
            t.SetException(ex);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(smBox.Continuation);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(smBox.Continuation);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }

    public struct GameTaskMethodBuilder<T>
    {
        private IStateMachineRunner smBox;
        private GameTask<T> task;
        public GameTask<T> Task => task;
        public static GameTaskMethodBuilder<T> Create() => new GameTaskMethodBuilder<T>();

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            task = GameTask<T>.Get();
            var box = StateMachineBox<TStateMachine>.Get();
            smBox = box;
            box.Init(ref stateMachine);
            smBox.Continuation();
        }

        public void SetResult(T result)
        {
            var t = task;
            smBox.Release();
            t.SetResult(result);
        }

        public void SetException(Exception ex)
        {
            var t = task;
            smBox.Release();
            t.SetException(ex);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(smBox.Continuation);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(smBox.Continuation);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }
}