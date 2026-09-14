using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTools.UISystem
{
    public class DynamicObjectPool<T> where T : class
    {
        private readonly List<T> m_List;
        private readonly Func<T> m_CreateFunc;
        private readonly Action<T> m_ActionOnGet;
        private readonly Action<T> m_ActionOnRelease;
        private readonly Action<T> m_ActionOnDestroy;
        private bool m_CollectionCheck;

        private readonly int retainedCount;
        private readonly int baseReleaseDelay;
        private readonly float releaseDelayDecay;
        private readonly int minDelay;
        private float timer;
        public int CountAll => CountActive + CountInactive;
        public int CountActive { get; private set; }
        public int CountInactive => m_List.Count;

        public DynamicObjectPool(
            Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnRelease = null,
            Action<T> actionOnDestroy = null,
            bool collectionCheck = true,
            int retainedCount = 2,
            int baseReleaseDelay = 60,
            float releaseDelayDecay = 0.9f,
            int minDelay = 5)
        {
            if (createFunc == null) throw new ArgumentNullException(nameof(createFunc));
            if (retainedCount < 0) throw new ArgumentOutOfRangeException(nameof(retainedCount));
            if (releaseDelayDecay <= 0 || releaseDelayDecay > 1) throw new ArgumentOutOfRangeException(nameof(releaseDelayDecay));
            if (minDelay > baseReleaseDelay) throw new ArgumentOutOfRangeException(nameof(minDelay));
            this.retainedCount = retainedCount;
            this.baseReleaseDelay = baseReleaseDelay;
            this.releaseDelayDecay = releaseDelayDecay;
            this.minDelay = minDelay;
            m_List = new List<T>(retainedCount);
            m_CreateFunc = createFunc;
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
            m_ActionOnDestroy = actionOnDestroy;
            m_CollectionCheck = collectionCheck;
        }

        public T Get()
        {
            CountActive++;
            T obj;
            if (m_List.Count == 0)
            {
                obj = m_CreateFunc();
            }
            else
            {
                int index = m_List.Count - 1;
                obj = m_List[index];
                m_List.RemoveAt(index);
            }
            m_ActionOnGet?.Invoke(obj);
            return obj;
        }

        public void Release(T element)
        {
            CountActive--;
            if (m_CollectionCheck && m_List.Count > 0)
            {
                for (int index = 0; index < m_List.Count; ++index)
                {
                    if (ReferenceEquals(element, (object)m_List[index])) throw new InvalidOperationException("Trying to release an object that has already been released to the pool.");
                }
            }
            m_ActionOnRelease?.Invoke(element);
            if (CountInactive < retainedCount || baseReleaseDelay > 0) m_List.Add(element);
            else
            {
                m_ActionOnDestroy?.Invoke(element);
            }
        }

        public void Update(float deltaTime)
        {
            int count = CountInactive;
            if (count <= retainedCount)
            {
                timer = 0;
                return;
            }
            timer += deltaTime;
            float currentDelay = Mathf.Max(minDelay, baseReleaseDelay * Mathf.Pow(releaseDelayDecay, count - retainedCount - 1));
            if (timer <= currentDelay) return;
            var element = m_List[0];
            m_List.RemoveAt(0);
            m_ActionOnDestroy?.Invoke(element);
            timer = 0;
        }

        public void Clear()
        {
            if (m_ActionOnDestroy != null)
            {
                foreach (T obj in m_List) m_ActionOnDestroy(obj);
            }
            m_List.Clear();
        }

        public void ClearToRetainedCount()
        {
            int removeCount = Mathf.Max(0, CountInactive - retainedCount);
            for (int i = 0; i < removeCount; i++)
            {
                int lastIndex = m_List.Count - 1;
                T item = m_List[lastIndex];
                m_List.RemoveAt(lastIndex);
                m_ActionOnDestroy?.Invoke(item);
            }
        }
    }
}