using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTools.FiniteStateMachine
{
    public interface IEventTrigger<in TEvent> { internal bool Trigger(TEvent eventName, ref bool canExit); }
    public interface IRecursiveEventTrigger { internal (bool canExit, int receiveCount, int triggerCount) RecursiveTrigger<TEvent>(TEvent eventName); }
    public abstract class FSMBase<TKey, TEvent> : IRecursiveEventTrigger, IEventTrigger<TEvent>
    {
        public bool isActive { get; private set; }
        private protected readonly Dictionary<TKey, IState> states = new();
        private readonly Dictionary<TKey, Transition<TKey, TEvent>> transitions = new();
        private readonly Transition<TKey, TEvent> anyTransitions = new();
        private readonly bool resumeLastState;
        private readonly TKey enterStateKey;

        private TKey currentStateKey;
        private IState currentState => states.GetValueOrDefault(currentStateKey);
        private Transition<TKey, TEvent> currentTransitions => transitions.GetValueOrDefault(currentStateKey);
        internal FSMBase(TKey enterStateKey, bool resumeLastState = false)
        {
            this.enterStateKey = enterStateKey;
            currentStateKey = enterStateKey;
            this.resumeLastState = resumeLastState;
        }
        public void AddState(TKey key, IState state)
        {
            if(isActive) { throw new InvalidOperationException("无法在激活状态机时添加状态"); }
            if (state == null) { throw new ArgumentNullException(nameof(state)); }
            if(state.isActive) { throw new InvalidOperationException("无法添加已经激活的状态"); }
            if (!states.TryAdd(key, state)) { throw new ArgumentException($"{key}对应的状态已经存在"); }
        }

        /// <summary> 添加条件转换 </summary>
        public void AddConditionTransition(TKey from, TKey to, Func<bool> condition, int priority = 0, bool force = false)
        {
            if (!transitions.TryGetValue(from, out Transition<TKey, TEvent> t))
            {
                t = new Transition<TKey, TEvent>();
                transitions[from] = t;
            }
            t.conditionTransitions.Add(to, condition, priority, true, force);
        }

        /// <summary> 添加条件转换 </summary>
        public void AddConditionTransition(TKey to, Func<bool> condition, int priority = 0, bool allowToSelf = false, bool force = false)
        {
            anyTransitions.conditionTransitions.Add(to, condition, priority, allowToSelf, force);
        }

        /// <summary> 添加事件转换 </summary>
        public void AddEventTransition(TKey from, TKey to, TEvent eventName, bool force = false)
        {
            if (!transitions.TryGetValue(from, out Transition<TKey, TEvent> t))
            {
                t = new Transition<TKey, TEvent>();
                transitions[from] = t;
            }
            t.eventTransitions.Add(to, eventName, true, force);
        }

        /// <summary> 添加事件转换 </summary>
        public void AddEventTransition(TKey to, TEvent eventName, bool allowToSelf = false, bool force = false)
        {
            anyTransitions.eventTransitions.Add(to, eventName, allowToSelf, force);
        }

        private bool ChangeState(TKey targetKey)
        {
            var exitState = currentState;
            exitState?.Exit();
            currentStateKey = targetKey;
            var enterState = currentState;
            bool canExit = enterState?.Enter() ?? true;
            return canExit;
        }

        /// <summary>允许递归直到某个状态处理了这个事件 </summary>
        (bool canExit, int receiveCount, int triggerCount) IRecursiveEventTrigger.RecursiveTrigger<T>(T eventName)
        {
            bool canExit = true;
            int receiveCount = 0;
            int triggerCount = 0;
            var currentState = this.currentState;
            if (currentState is StateBase progress)
            {
                canExit = progress.canExit;
            }
            else if(currentState is MonoStateBase mProgress)
            {
                canExit = mProgress.canExit;
            }
            else if (currentState is IRecursiveEventTrigger tirrger)
            {
                var result = tirrger.RecursiveTrigger(eventName);
                triggerCount = result.triggerCount;
                canExit = canExit && result.canExit;
                receiveCount = result.receiveCount;
            }
            else if (currentState != null) throw new Exception("IState 既不是 State 也不是 FSM");
            if (this is IEventTrigger<T> trigger)
            {
                receiveCount++;
                if (trigger.Trigger(eventName, ref canExit)) { triggerCount++; }
            }
            return (canExit, receiveCount, triggerCount);
        }

        /// <summary> 触发事件 </summary>
        bool IEventTrigger<TEvent>.Trigger(TEvent eventName, ref bool canExit)
        {
            var stateKey = currentStateKey;
            if (anyTransitions.eventTransitions.Check(ref stateKey, canExit, eventName))
            {
                canExit = ChangeState(stateKey);
                return true;
            }
            else
            {
                var currentT = currentTransitions;
                if (currentT != null && currentT.eventTransitions.Check(ref stateKey, canExit, eventName))
                {
                    canExit = ChangeState(stateKey);
                    return true;
                }
            }
            return false;
        }

        private bool TryConditionTransition(ref bool canExit)
        {
            var stateKey = currentStateKey;
            if (anyTransitions.conditionTransitions.Check(ref stateKey, canExit)) 
            { 
                canExit = ChangeState(stateKey);
                return true;
            }
            else
            {
                var currentT = currentTransitions;
                if (currentT != null && currentT.conditionTransitions.Check(ref stateKey, canExit)) 
                {
                    canExit = ChangeState(stateKey);
                    return true;
                }
            }
            return false;
        }

        private protected bool OnEnter()
        {   
            if (isActive) { throw new InvalidOperationException("FSM is already active."); }
            isActive = true;
            if (!resumeLastState) { currentStateKey = enterStateKey; }
            bool canExit = currentState?.Enter() ?? true;
            return canExit;
        }

        private protected bool OnUpdate()
        {
            if (!isActive) { throw new InvalidOperationException("FSM is not active."); }
            bool canExit = currentState?.Update() ?? true;
            TryConditionTransition(ref canExit);
            return canExit;
        }

        private protected void OnExit() 
        { 
            if (!isActive) { throw new InvalidOperationException("FSM is not active."); }
            currentState?.Exit();
            isActive = false;
        }
    }
}
