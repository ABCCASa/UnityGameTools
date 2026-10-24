using System;
using System.Reflection;
using GameTools.DataBindSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameTools.UISystem
{
    public abstract class UGUIScreenBase : ScreenBase
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;

        protected float alpha
        {
            get => canvasGroup.alpha;
            set => canvasGroup.alpha = value;
        }

        private protected sealed override void SetInteractable(bool value) => canvasGroup.interactable = value;
        private protected override void SetOrder(int order) => canvas.sortingOrder = order;

        #region 绑定系统

        private protected override void OnBeforeDispose()
        {
            unbindOnClose?.Invoke();
        }

        private protected override void OnBeforeInit()
        {
            BindInit();
            bindOnOpen?.Invoke();
        }

        protected virtual bool useAutoBind => true;

        protected T Find<T>(string path = null, bool throwOnNotFound = true) where T : Component
        {
            Transform trans = Find(path, throwOnNotFound);
            T target = trans?.GetComponent<T>();
            if (target != null) return target;
            if (throwOnNotFound) throw new Exception($"cannot find the component({typeof(T)}) in path({path})");
            return null;
        }

        protected Transform Find(string path = null, bool throwOnNotFound = true)
        {
            if (string.IsNullOrEmpty(path)) return transform;
            Transform target = transform.Find(path);
            if (target != null) return target;
            if (throwOnNotFound) throw new Exception($"cannot find the object in path({path})");
            return null;
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
        protected sealed class AutoBindAttribute : Attribute
        {
            public readonly string path;

            public AutoBindAttribute(string path)
            {
                this.path = path;
            }
        }

        private Action bindOnOpen, unbindOnClose;

        protected void RegisterBindAction(Action bindAction, Action unbindAction)
        {
            bindOnOpen += bindAction;
            unbindOnClose += unbindAction;
        }

        private void BindInit()
        {
            if (!useAutoBind) return;
            BindFields();
            BindMethods();
        }

        private void BindFields()
        {
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (!field.IsDefined(typeof(AutoBindAttribute), true)) continue;

                if (field.GetValue(this) is IBindWithGameObject bind)
                {
                    foreach (var attr in field.GetCustomAttributes<AutoBindAttribute>(true))
                    {
                        string path = attr.path;
                        GameObject trans = Find(path).gameObject;
                        var bind1 = bind;
                        RegisterBindAction(() =>
                            {
                                var (find, process) = bind1.Bind(trans.gameObject);
                                if (find == 0)
                                {
                                    throw new Exception($"fail to bind with {path}");
                                }
                            },
                            () => bind.Unbind(trans));
                    }
                }
                else throw new Exception($"field: {field.Name}未继承IBindToGameObject接口无法实现自动绑定");
            }
        }

        private void BindMethods()
        {
            var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes<AutoBindAttribute>(true);
                foreach (var attr in attributes)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        Button button = Find<Button>(attr.path);
                        UnityAction action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), this, method);
                        RegisterBindAction(
                            () => button.onClick.AddListener(action),
                            () => button.onClick.RemoveListener(action));
                    }
                    else if (parameters.Length == 1)
                    {
                        var parameter = parameters[0].ParameterType;
                        if (parameter.IsByRef) throw new Exception("参数带 ref/in/out，无法绑定");
                        if (parameter == typeof(float))
                        {
                            Slider slider = Find<Slider>(attr.path);
                            UnityAction<float> action =
                                (UnityAction<float>)Delegate.CreateDelegate(typeof(UnityAction<float>), this, method);
                            RegisterBindAction(
                                () => slider.onValueChanged.AddListener(action),
                                () => slider.onValueChanged.RemoveListener(action));
                        }
                        else if (parameter == typeof(bool))
                        {
                            Toggle toggle = Find<Toggle>(attr.path);
                            UnityAction<bool> action =
                                (UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>), this, method);
                            RegisterBindAction(
                                () => toggle.onValueChanged.AddListener(action),
                                () => toggle.onValueChanged.RemoveListener(action));
                        }
                        else if (parameter == typeof(string))
                        {
                            TMP_InputField inputField = Find<TMP_InputField>(attr.path);
                            UnityAction<string> action = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), this, method);
                            RegisterBindAction(
                                () => inputField.onValueChanged.AddListener(action),
                                () => inputField.onValueChanged.RemoveListener(action));
                        }
                        else if (parameter == typeof(int))
                        {
                            TMP_Dropdown dropdown = Find<TMP_Dropdown>(attr.path);
                            UnityAction<int> action = (UnityAction<int>)Delegate.CreateDelegate(typeof(UnityAction<int>), this, method);
                            RegisterBindAction(
                                () => dropdown.onValueChanged.AddListener(action),
                                () => dropdown.onValueChanged.RemoveListener(action));
                        }
                        else
                        {
                            throw new Exception($"Method: {method} 不支持自动绑定");
                        }
                    }
                    else // > 1
                    {
                        throw new Exception($"Method: {method} 不支持自动绑定");
                    }
                }
            }
        }

        #endregion
    }
}