
using System;
using UnityEngine;

namespace GameTools.FiniteStateMachine
{
    public interface IState
    {
        bool isActive { get; }
        internal bool Enter();
        internal bool Update();
        internal void Exit();
    }

    public abstract class StateBase : IState
    {
        public bool isActive { get; private set; }
        internal bool canExit { get; private set; }
        protected virtual bool OnEnter() { return true; }
        protected virtual bool OnUpdate() { return true; }
        protected virtual void OnExit() { }
        bool IState.Enter()
        {
            if(isActive) throw new Exception("State is already active.");
            isActive = true;
            canExit = OnEnter();
            return canExit;
        }
        bool IState.Update()
        {
            if (!isActive) throw new Exception("State is not active.");
            canExit = OnUpdate();
            return canExit;
        }
        void IState.Exit()
        {
            if (!isActive) throw new Exception("State is not active.");
            OnExit();
            isActive = false;
        }
    }


    public abstract class MonoStateBase : MonoBehaviour, IState
    {
        public bool isActive { get; private set; }
        internal bool canExit { get; private set; }
        protected virtual bool OnEnter() { return true; }
        protected virtual bool OnUpdate() { return true; }
        protected virtual void OnExit() { }
        bool IState.Enter()
        {
            if (isActive) throw new Exception("State is already active.");
            isActive = true;
            canExit = OnEnter();
            return canExit;
        }
        bool IState.Update()
        {
            if (!isActive) throw new Exception("State is not active.");
            canExit = OnUpdate();
            return canExit;
        }
        void IState.Exit()
        {
            if (!isActive) throw new Exception("State is not active.");
            OnExit();
            isActive = false;
        }
    }

    public sealed class State : StateBase
    {
        private Func<bool> onEnter, onUpdate;
        private Action onExit;
        public State(Func<bool> onEnter = null, Func<bool> onUpdate = null, Action onExit = null)
        {
            this.onEnter = onEnter;
            this.onUpdate = onUpdate;
            this.onExit = onExit;
        }
        protected override bool OnEnter() => onEnter?.Invoke() ?? true;  
        protected override bool OnUpdate() => onUpdate?.Invoke() ?? true;
        protected override void OnExit() => onExit?.Invoke();
    }
}