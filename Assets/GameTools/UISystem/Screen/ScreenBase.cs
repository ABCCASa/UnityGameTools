using System;
using System.Reflection;
using GameTools.DataBindSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace GameTools.UISystem
{
    public enum ScreenState { Open, Close, Pause }
    [DisallowMultipleComponent]
    public abstract class ScreenBase: MonoBehaviour, IContainerItem 
    {
        internal ScreenBase() { }
        private ScreenContainer parentContainer;
        private bool _globalInteractable, _selfInteractable;
        private bool globalInteractable
        {
            set
            {
                _globalInteractable = value;
                SetInteractable(_globalInteractable && _selfInteractable);
            }
        }

        private bool selfInteractable
        {
            set
            {
                _selfInteractable = value;
                SetInteractable(_globalInteractable && _selfInteractable);
            }
        }


        public ScreenState state { get; private set; } = ScreenState.Close;
        public bool isFade { get; private set; } = false;
        protected abstract bool blockInput { get; }


        protected virtual void OnInit() { }
        protected virtual void OnClose() { }
        protected virtual void OnPause() { }
        protected virtual void OnResume() { }
        protected virtual void OnDispose() { }

        public void CompleteAnimation()
        {
            if (isFade) UIAnimationManager.Instance.CompleteAnimation(this);
        }

        public void SpeedUpAnimation(float fadeTime)
        {
            if (isFade) UIAnimationManager.Instance.SpeedUpAnimation(this, fadeTime);
        }

        internal void SetInit()
        {
            selfInteractable = false;
            globalInteractable = false;
            gameObject.SetActive(false); // 默认关闭，所以先要隐藏
            SafeCall(OnInit);
        }

        internal void SetDispose()
        {
            if (state != ScreenState.Close) throw new InvalidOperationException("cannot dispose a screen when state is not close");
            SafeCall(OnDispose);
            Destroy(gameObject);
        }
        

        private protected void SetOpen(Action onOpen, float fadeTime, ScreenContainer container, string animKey = null, Action callback = null)
        {
            if (state != ScreenState.Close) throw new InvalidOperationException("cannot open screen when state is not close");
            CompleteAnimation();
            state = ScreenState.Open;
            gameObject.SetActive(true);
            parentContainer = container; 
            SafeCall(onOpen);
            isFade = true;
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, true,
                (progress) => Animation(animKey, progress),
                () =>
                {
                    isFade = false;
                    selfInteractable = true;
                    callback?.Invoke();
                });
        }
        
        internal void SetClose(float fadeTime = -1, string animKey = null, Action callback = null)
        {
            if (state == ScreenState.Close) throw new InvalidOperationException("cannot close screen when state is close");
            CompleteAnimation();
            ScreenState previousState = state;
            state = ScreenState.Close;
            selfInteractable = false;
            SafeCall(OnClose);
            parentContainer = null;
            if (previousState == ScreenState.Open)
            {
                isFade = true;
                UIAnimationManager.Instance.SetAnimation(this, fadeTime, false,
                    (progress) => Animation(animKey, progress),
                    () =>
                    {
                        isFade = false;
                        gameObject.SetActive(false);
                        callback?.Invoke();
                    });
            }
            else
            {
                gameObject.SetActive(false);
                callback?.Invoke();
            }
        }

        internal void SetPause(float fadeTime = -1, string animKey = null, Action callback = null)
        {
            if (state != ScreenState.Open) throw new InvalidOperationException("cannot pause screen when state is not open");
            CompleteAnimation();
            state = ScreenState.Pause;
            selfInteractable = false;
            SafeCall(OnPause);
            isFade = true;
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, false,
                (progress) => Animation(animKey, progress),
                () =>
                {
                    isFade = false;
                    gameObject.SetActive(false);
                    callback?.Invoke();
                });
        }

        internal void SetResume(float fadeTime = -1, string animKey = null, Action callback = null)
        {
            if (state != ScreenState.Pause) throw new InvalidOperationException("cannot resume screen when state is not pause");
            CompleteAnimation();
            state = ScreenState.Open;
            gameObject.SetActive(true);
            SafeCall(OnResume);
            isFade = true;
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, true,
                (progress) => Animation(animKey, progress),
                () =>
                {
                    isFade = false;
                    selfInteractable = true;
                    callback?.Invoke();
                });
        }
     
        private void SafeCall(Action action)
        {
            try { action?.Invoke();}
            catch (Exception e) {Debug.LogException(e, this);  }
        }

        protected abstract void Animation(string animKey, float progress);
        protected abstract void SetInteractable(bool value);
        protected abstract void SetOrder(int order);
        void IContainerItem.UpdateInteractable(ref bool value)
        {
            if (state != ScreenState.Open && !isFade) return;
            globalInteractable = value;
            if (blockInput) value = false;
        }

        void IContainerItem.UpdateOrder(ref int order)
        {
            SetOrder(order);
            order++;
        }

        #region 访问Container
        public T OpenScreen<T>(float fadeTime, string animKey = null, bool addAbove = true, bool relative = false) where T : Screen
        {
            return parentContainer.Open<T>(fadeTime, animKey, addAbove, relative?this:null);
        }
        
        public TScreen OpenScreen<TScreen, TParam>(TParam param, float fadeTime, string animKey = null, bool addAbove = true, bool relative = false) where TScreen : Screen<TParam>
        {
            return parentContainer.Open<TScreen, TParam>(param, fadeTime, animKey, addAbove, relative?this:null);
        }
        
        public void ChangeOrder(bool addAbove = true, ScreenBase relative = null) =>  parentContainer.ChangeOrder(this, addAbove, relative);

        public void Pause(float fadeTime = -1, string animKey = null) => parentContainer.Pause(this, fadeTime, animKey);

        public void Resume(float fadeTime = -1, string animKey = null) => parentContainer.Resume(this, fadeTime, animKey);
        
        public void Close(float fadeTime, string animKey) => parentContainer.Close(this, fadeTime, animKey);
        
        #endregion
        /*#region 绑定系统
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    protected sealed class AutoBindAttribute : Attribute
    {
        public readonly string path;

        public AutoBindAttribute(string path)
        {
            this.path = path;
        }
    }
        protected Transform Find(string path = null)
        {
            return string.IsNullOrEmpty(path) ? transform : transform.Find(path);
        }

        protected T Find<T>(string path = null) where T : Component
        {
            Transform trans = Find(path);
            if (trans == null) return null;
            T target = trans.GetComponent<T>();
            return target;
        }


        private Action bindAction, unbindAction;

        protected void RegisterBindAction(Action bindAction, Action unbindAction)
        {
            this.bindAction += bindAction;
            this.unbindAction += unbindAction;
        }

        private void BindInit()
        {
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
                        Transform trans = Find(path);
                        if (trans == null)
                        {
                            Debug.LogError($"fail to bind with {attr.path}");
                            continue;
                        }

                        GameObject obj = Find(path).gameObject;
                        var bind1 = bind;
                        RegisterBindAction(() =>
                            {
                                var (find, process) = bind1.Bind(obj);
                                if (find == 0) Debug.LogError($"fail to bind with {path}");
                            },
                            () => bind.Unbind(obj));
                    }
                }
                else Debug.LogError($"field: {field.Name} 未继承IBindToGameObject接口无法实现自动绑定");
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
                        if (button == null)
                        {
                            Debug.LogError($"fail to bind with {attr.path}");
                            continue;
                        }

                        UnityAction action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), this, method);
                        RegisterBindAction(
                            () => button.onClick.AddListener(action),
                            () => button.onClick.RemoveListener(action));
                    }
                    else if (parameters.Length == 1)
                    {
                        var parameter = parameters[0].ParameterType;
                        if (parameter.IsByRef)
                        {
                            Debug.LogError("参数带 ref/in/out，无法绑定");
                            continue;
                        }

                        if (parameter == typeof(float))
                        {
                            Slider slider = Find<Slider>(attr.path);
                            if (slider == null)
                            {
                                Debug.LogError($"fail to bind with {attr.path}");
                                continue;
                            }

                            var action = (UnityAction<float>)Delegate.CreateDelegate(typeof(UnityAction<float>), this, method);
                            RegisterBindAction(
                                () => slider.onValueChanged.AddListener(action),
                                () => slider.onValueChanged.RemoveListener(action));
                        }
                        else if (parameter == typeof(bool))
                        {
                            Toggle toggle = Find<Toggle>(attr.path);
                            if (toggle == null)
                            {
                                Debug.LogError($"fail to bind with {attr.path}");
                                continue;
                            }

                            var action = (UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>), this, method);
                            RegisterBindAction(
                                () => toggle.onValueChanged.AddListener(action),
                                () => toggle.onValueChanged.RemoveListener(action));
                        }
                        else if (parameter == typeof(string))
                        {
                            TMP_InputField inputField = Find<TMP_InputField>(attr.path);
                            if (inputField == null)
                            {
                                Debug.LogError($"fail to bind with {attr.path}");
                                continue;
                            }

                            var action = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), this, method);
                            RegisterBindAction(
                                () => inputField.onValueChanged.AddListener(action),
                                () => inputField.onValueChanged.RemoveListener(action));
                        }
                        else if (parameter == typeof(int))
                        {
                            TMP_Dropdown dropdown = Find<TMP_Dropdown>(attr.path);
                            if (dropdown == null)
                            {
                                Debug.LogError($"fail to bind with {attr.path}");
                                continue;
                            }

                            var action = (UnityAction<int>)Delegate.CreateDelegate(typeof(UnityAction<int>), this, method);
                            RegisterBindAction(
                                () => dropdown.onValueChanged.AddListener(action),
                                () => dropdown.onValueChanged.RemoveListener(action));
                        }
                        else Debug.LogError($"Method: {method} 不支持自动绑定");
                    }
                    else Debug.LogError($"Method: {method} 不支持自动绑定, 参数大于1"); // > 1
                }
            }
        }

        #endregion*/
    }
}