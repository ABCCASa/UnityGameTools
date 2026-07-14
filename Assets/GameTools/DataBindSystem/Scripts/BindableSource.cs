using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTools.DataBindSystem
{
    public interface IBindWithGameObject
    {
        (int find, int process) Unbind(GameObject obj);
        (int find, int process) Bind(GameObject obj);
    }

    public class BindableSource<T>: IBindWithGameObject
    {
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        private readonly List<WeakReference<IBindingTarget<T>>> targets = new();
        private Action<T> onValueChange;
        private static bool IsAliveTarget(IBindingTarget<T> target)
        {
            return target is UnityEngine.Object obj ? obj != null : target != null;
        }
        public T Value
        {
            get => getter();
            set
            {
                var oldValue = getter();
                if (EqualityComparer<T>.Default.Equals(oldValue, value)) { return; }
                setter(value);
                for (int i = targets.Count - 1; i >= 0; i--)
                {
                    if (targets[i].TryGetTarget(out var element) && IsAliveTarget(element))
                    {
                        element.OnSourceChange(getter());
                    }
                    else 
                    {
                        Debug.LogWarning("出现未解绑就被销毁/释放的组件");
                        targets.RemoveAt(i);
                    }
                }
                onValueChange?.Invoke(getter());
            }
        }

        public BindableSource(Func<T> getter, Action<T> setter)
        {
            this.getter = getter ?? throw new ArgumentNullException(nameof(getter));
            this.setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }

        public BindableSource(T value)
        {
            getter = () => value;
            setter = (v) => value = v;
        }

        public void RemoveListener(Action<T> onValueChange)
        { if (onValueChange == null) return;
            this.onValueChange -= onValueChange;
        }

        public void AddListener(Action<T> onValueChange)
        {
            if (onValueChange == null) return;
            onValueChange.Invoke(getter());
            this.onValueChange += onValueChange;
        }

        public bool Bind(IBindingTarget<T> bindTarget)
        {
            if (bindTarget == null)
            {
                Debug.LogError("要绑定的目标不可为null");
                return false;
            }
            if (bindTarget.isBind)
            {
                Debug.LogError($"{bindTarget} 已经绑定了其他的参数，无法再次绑定");
                return false;
            }
            targets.Add(new WeakReference<IBindingTarget<T>>(bindTarget));
            bindTarget.OnBind(this);
            return true;
        }

        public bool Unbind(IBindingTarget<T> unbindTarget)
        {
            if (unbindTarget == null)
            {
                Debug.LogError($"{nameof(unbindTarget)}不可以为null");
                return false;
            }

            if (!unbindTarget.isBind) {
                Debug.LogError($"{unbindTarget}未绑定任何参数");
                return false;
            }

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (targets[i].TryGetTarget(out var element) && IsAliveTarget(element))
                {
                    if (element == unbindTarget) 
                    {
                        targets.RemoveAt(i);
                        unbindTarget.OnUnbind();
                        return true;
                    }
                }
                else
                {
                    Debug.LogWarning("出现未解绑就被销毁/释放的的组件");
                    targets.RemoveAt(i);
                    continue;
                }
            }
            Debug.LogError($"{unbindTarget}未和{this}绑定，无法解除绑定");
            return false;
        }

        public void UnbindAll()
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (targets[i].TryGetTarget(out var element) && IsAliveTarget(element))
                {
                    element.OnUnbind();
                }
            }
            targets.Clear();
        }
        
        public static implicit operator T(BindableSource<T> bindableBool)
        {
            return bindableBool.Value;
        }

        (int find, int process) IBindWithGameObject.Bind(GameObject obj)
        {
            int bindCount = 0;
            var items =  obj.GetComponents<IBindingTarget<T>>();
            foreach (var item in items)
            {
                if (Bind(item)) bindCount++;
                else Debug.LogError($"{item} 已经绑定了其他source");
            }
            return (items.Length ,bindCount);
        }
        
        (int find, int process) IBindWithGameObject.Unbind(GameObject obj)
        {
            int unbindCount = 0;
            var items =  obj.GetComponents<IBindingTarget<T>>();
            foreach (var item in items)
            {
                if (Unbind(item)) { unbindCount++; }
                else { Debug.LogError($"{item} 绑定了其他source"); }
            }
            return (items.Length ,unbindCount);
        }

       
    }
}