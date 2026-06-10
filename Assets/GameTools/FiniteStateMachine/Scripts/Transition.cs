using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTools.FiniteStateMachine
{
    public class ConditionTransition<StateKey>
    {
       private int conditionTimeStamp = 0;
        private readonly SortedList<(int priority, int timeStamp), (Func<bool> condition, StateKey to, bool allowToSelf, bool force)> conditionTransitions =
            new(Comparer<(int priority, int timeStamp)>.Create((x, y) =>
            {
                int cmp = y.priority.CompareTo(x.priority); // priority 降序
                if (cmp != 0) return cmp;
                return x.timeStamp.CompareTo(y.timeStamp);  // timeStamp 升序
            }));
        public void Add(StateKey targetKey, Func<bool> condition, int priority=0, bool allowToSelf=true, bool force=false)
        {
            if (condition == null && force) { Debug.LogWarning("添加的转换没有条件并且是强制转换"); }
            conditionTimeStamp++;
            conditionTransitions[(priority, conditionTimeStamp)] = (condition, targetKey, allowToSelf, force);
        }

        public bool Check(ref StateKey state, bool canExit)
        {
            for (int i = 0; i < conditionTransitions.Count; i++)
            {
                var (condition, to, allowToSelf, force) = conditionTransitions.Values[i];
                if (!allowToSelf && EqualityComparer<StateKey>.Default.Equals(state, to)) { continue; }
                if (!canExit && !force) { continue; }
                if (condition?.Invoke() ?? true)
                {
                    state = to;
                    return true;
                }
            }
            return false;
        }
    }

    public class EventTransition<StateKey, EventName>
    {
        private readonly Dictionary<EventName, (StateKey to, bool allowToSelf, bool force)> eventTransitions = new();
        public void Add(StateKey to, EventName eventName, bool allowToSelf=true, bool force = false)
        {
            if (eventTransitions.ContainsKey(eventName)) { throw new Exception($"{eventName} 已经被添加，无法重复添加"); }
            eventTransitions[eventName] = (to, allowToSelf, force);
        }
        public bool Check(ref StateKey state, bool canExit, EventName eventName)
        {
            if (eventTransitions.TryGetValue(eventName, out var item))
            {
                if (item.allowToSelf || !EqualityComparer<StateKey>.Default.Equals(state, item.to))
                {
                    if (canExit || item.force)
                    {
                        state = item.to;
                        return true;
                    }
                }
            }
            return false;
        }
    }
    public class Transition<StateKey, EventName>
    {
        public readonly ConditionTransition<StateKey> conditionTransitions = new();
        public readonly EventTransition<StateKey, EventName> eventTransitions = new();
    }
}
