
using System;
using UnityEngine;
namespace GameTools.DataBindSystem
{
    public interface IBindingTarget<T>
    {
        bool isBind { get; }

        /// <summary> 由 Bind Value 来调用，自身不会被调用 </summary>
        internal void OnBind(BindableSource<T> value);

        /// <summary> 由 Bind Value 来调用，自身不会被调用 </summary>
        internal void OnUnbind();

        /// <summary>在source 发生改变时触发 </summary>
        internal void OnSourceChange(T value);
    }

    public abstract class BindingTarget<T> : MonoBehaviour, IBindingTarget<T>
    {
        private BindableSource<T> bindSource;
        protected T sourceValue 
        {
            get => bindSource.Value;
            set => bindSource.Value = value;
        }
        public bool isBind => bindSource != null;
        protected abstract void OnBind();
        protected abstract void OnUnbind();
        protected abstract void OnSourceChange(T value);

        void IBindingTarget<T>.OnBind(BindableSource<T> value)
        {
            bindSource = value;
            OnBind();
            OnSourceChange(value.Value);
        }

        void IBindingTarget<T>.OnUnbind()
        {
            bindSource = null;
            OnUnbind();
        }
        void IBindingTarget<T>.OnSourceChange(T value)
        {
            OnSourceChange(value);
        }

        #region 反向调用
        public bool Bind(BindableSource<T> source) 
        {
            if (source == null)
            {
                Debug.LogError("要绑定的参数不可为null");
                return false;
            }
            return source.Bind(this);  
        }

        public bool Unbind()
        {
            if (bindSource == null)
            {
                Debug.LogWarning($"未绑定参数，无需解绑");
                return false;
            }
            return bindSource.Unbind(this);
        }
        #endregion


        private void OnDestroy()
        {
            if (isBind) bindSource.Unbind(this);
        }
    }

    public abstract class BindingTarget<TComponent, T> : BindingTarget<T> where TComponent : Component
    {
        [SerializeField] private TComponent _component;
        protected TComponent component
        {
            get
            {
                if (_component == null) _component = GetComponent<TComponent>();
                return _component;
            }
        }
        private void Reset()
        {
            _component = GetComponent<TComponent>();
        }
    }
}