
using UnityEngine;
namespace GameTools.FiniteStateMachine
{
    public sealed class SubFSM<TKey, TEvent> : FSMBase<TKey, TEvent>, IState
    {
        public SubFSM(TKey enterStateKey, bool resumeLastState = false) : base(enterStateKey, resumeLastState) {}
        bool IState.Enter() => OnEnter();
        bool IState.Update() => OnUpdate();
        void IState.Exit() => OnExit();
    }

    public sealed class RootFSM<StateKey, EventName> : FSMBase<StateKey, EventName>
    {
        public RootFSM(StateKey enterStateKey, bool resumeLastState = false) : base(enterStateKey, resumeLastState) {  } 

        public void Tick()
        {
            if (!isActive) { OnEnter(); }
            OnUpdate();
        }
      
        public void Exit()
        {
            if (!isActive) { return; }
            OnExit();
        }
        public int Trigger<T>(T eventName)
        {
            if (!isActive) { return 0; }
            var (_, receiveCount, triggerCount) = ((IRecursiveEventTrigger)this).RecursiveTrigger(eventName);
            if (receiveCount <= 0) {
                Debug.LogWarning($"{this}未包含支持的{typeof(T)}的事件触发器");
            }
            return triggerCount;
        }
    }
}
