using UnityEngine;
using GameTools.PlayerLoopManagement;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.PlayerLoop;

namespace GameTools.GameTask
{
    public class PlayerLoopAwaitable: IAwaiter
    {
        private protected PlayerLoopAwaitable() { }
        private Queue<Action> nextQueue = new(128);
        private Queue<Action> currentQueue = new(128);
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
    
    internal class PlayerLoopAwaitable<TLoop> : PlayerLoopAwaitable
    {
        private struct Before { }
        private struct After { }
        internal PlayerLoopAwaitable()
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
        public static PlayerLoopAwaitable update;
        public static PlayerLoopAwaitable fixedUpdate;
        public static PlayerLoopAwaitable lateUpdate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Init()
        {
            update = new PlayerLoopAwaitable<Update.ScriptRunBehaviourUpdate>();
            fixedUpdate = new PlayerLoopAwaitable<FixedUpdate.ScriptRunBehaviourFixedUpdate>();
            lateUpdate = new PlayerLoopAwaitable<PreLateUpdate.ScriptRunBehaviourLateUpdate>();
            Application.quitting += () =>
            {
                update.Clear();
                fixedUpdate.Clear();
                lateUpdate.Clear();
            };
        }
    }
}