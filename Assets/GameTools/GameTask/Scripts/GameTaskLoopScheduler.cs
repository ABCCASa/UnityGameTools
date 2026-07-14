using UnityEngine;
using GameTools.PlayerLoopManagement;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.PlayerLoop;


namespace GameTools.GameTask
{
    public class AsyncLoop: IAwaiter
    {
        internal AsyncLoop() { }
        private Queue<Action> nextQueue = new Queue<Action>(128);
        private Queue<Action> currentQueue = new Queue<Action>(128);
        protected void BeforeAction()
        {
            (currentQueue, nextQueue) = (nextQueue, currentQueue); // Swap Queues
        }

        protected void AfterAction()
        {
            while (currentQueue.Count > 0)
            {
                var action = currentQueue.Dequeue();
                try { action(); } catch (Exception e) { Debug.LogException(e); }
            }
        }

        public IAwaiter GetAwaiter() => this;
        bool IAwaiter.IsCompleted => false;
        void IAwaiter.GetResult() {}
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => nextQueue.Enqueue(continuation);
        void INotifyCompletion.OnCompleted(Action continuation) => nextQueue.Enqueue(continuation);

        internal void Clear()
        {
            nextQueue.Clear();
            currentQueue.Clear();
        }
    }

    public class AsyncLoop<TLoop> : AsyncLoop
    {
        internal struct Before { }
        internal struct After { }
        internal AsyncLoop()
        {
            if (PlayerLoopManager.Exist<Before>() || PlayerLoopManager.Exist<After>())
            {
                throw new InvalidOperationException("player loop 重复添加");
            }
            if (!PlayerLoopManager.InsertAdjacentTo<Before, TLoop>(BeforeAction, false))
            {
                throw new InvalidOperationException($"Failed to insert before {typeof(TLoop).Name}");
            }
            if (!PlayerLoopManager.InsertAdjacentTo<After, TLoop>(AfterAction, true))
            {
                throw new InvalidOperationException($"Failed to insert after {typeof(TLoop).Name}");
            }
        }
    }


    internal static class GameTaskLoopScheduler
    {
        public static AsyncLoop update;
        public static AsyncLoop fixedUpdate;
        public static AsyncLoop lateUpdate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Init()
        {
            update = new AsyncLoop<Update.ScriptRunBehaviourUpdate>();
            fixedUpdate = new AsyncLoop<FixedUpdate.ScriptRunBehaviourFixedUpdate>();
            lateUpdate = new AsyncLoop<PreLateUpdate.ScriptRunBehaviourLateUpdate>();
            Application.quitting += () =>
            {
                update.Clear();
                fixedUpdate.Clear();
                lateUpdate.Clear();
            };
        }
    }
}