using System;
using System.Reflection;
using GameTools.DataBindSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameTools.UISystem
{
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AutoBindAttribute : Attribute
    {
        public readonly string path;
        public AutoBindAttribute(string path)
        {
            this.path = path;
        }
    }
    
    public enum ScreenAnimType { Open, Pause, Resume, Close }
    public enum ScreenState { Open, Close, Pause }

    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public abstract class ScreenBase: MonoBehaviour, IContainerItem 
    {
        internal ScreenBase() { }
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        private ContainerBase parentContainer;
        private bool _globalInteractable, _selfInteractable;
        private bool globalInteractable
        {
            set
            {
                _globalInteractable = value;
                canvasGroup.interactable = _globalInteractable && _selfInteractable;
            }
        }
        private protected bool selfInteractable
        {
            set
            {
                _selfInteractable = value;
                canvasGroup.interactable = _globalInteractable && _selfInteractable;
            }
        }

        public ScreenState state { get; private set; } = ScreenState.Close;
        public bool isFade { get; private set; }
        protected abstract bool blockInput { get; }
        protected virtual bool enableAutoBind => true;

        protected virtual void OnInit() { }
        protected virtual void OnClose(ScreenState previousState) { }
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
            if (enableAutoBind) BindInit();
            bindAction?.Invoke();
        }

        internal void SetDispose()
        {
            if (state != ScreenState.Close) throw new InvalidOperationException("cannot dispose a screen when state is not close");
            SafeCall(OnDispose);
            unbindAction?.Invoke();
            Destroy(gameObject);
        }

        internal void SetPause(float fadeTime = -1, Action callback = null)
        {
            if (state != ScreenState.Open) throw new InvalidOperationException("cannot pause screen when state is not open");
            CompleteAnimation();
            state = ScreenState.Pause;
            selfInteractable = false;
            SafeCall(OnPause);
            isFade = true;
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, true,
                (progress) => Animation(ScreenAnimType.Pause, progress),  
                () =>
                {
                isFade = false;
                gameObject.SetActive(false);
                callback?.Invoke();
                });
        }

        internal void SetResume(float fadeTime = -1, Action callback = null)
        {
            if (state != ScreenState.Pause) throw new InvalidOperationException("cannot resume screen when state is not pause");
            CompleteAnimation();
            state = ScreenState.Open;
            gameObject.SetActive(true);
            SafeCall(OnResume);
            isFade = true;
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, true, 
                (progress) => Animation(ScreenAnimType.Resume, progress), 
                () => 
                {
                isFade = false;
                selfInteractable = true;
                callback?.Invoke();
                });
        }

        internal void SetClose(float fadeTime = -1, Action callback = null)
        {
            if (state == ScreenState.Close) throw new InvalidOperationException("cannot close screen when state is close");
            CompleteAnimation();
            parentContainer = null;
            ScreenState previousState = state;
            state = ScreenState.Close;
            selfInteractable = false;
            SafeCall(()=>OnClose(previousState));
            if (previousState == ScreenState.Open)
            {
                isFade = true;
                UIAnimationManager.Instance.SetAnimation(this, fadeTime, true, 
                    (progress) => Animation(ScreenAnimType.Close, progress),
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

        private protected void SetOpen(Action onOpen, float fadeTime, ContainerBase container, Action callback = null)
        {
            if (state != ScreenState.Close) throw new InvalidOperationException("cannot open screen when state is not close");
            CompleteAnimation();
            state = ScreenState.Open;
            gameObject.SetActive(true);
            SafeCall(onOpen);
            this.parentContainer = container; // 延后保存container，防止onOpen时尝试打开其他screen(container在这时处于busy状态)
            isFade = true;
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, true,
                (progress) => Animation(ScreenAnimType.Open, progress), 
                () =>
                {
                    isFade = false;
                    selfInteractable = true;
                    callback?.Invoke();
                });
        }
        
        private void SafeCall(Action action)
        {
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        protected virtual void Animation(ScreenAnimType animType, float progress)
        {
            canvasGroup.alpha = animType switch
            {
                ScreenAnimType.Open => progress,
                ScreenAnimType.Pause => 1,
                ScreenAnimType.Resume => 1,
                ScreenAnimType.Close => 1 - progress,
                _ => throw new ArgumentOutOfRangeException(nameof(animType), animType, null)
            };
        }

        void IContainerItem.SetInteractable(ref bool value) => SetInteractable(ref value);
        void IContainerItem.SetOrder(ref int order) => SetOrder(ref order);

        private protected virtual void SetInteractable(ref bool value)
        {
            if (state != ScreenState.Open && !isFade) return;
            globalInteractable = value;
            if (blockInput) value = false;
        }

        private protected virtual void SetOrder(ref int order)
        {
            canvas.sortingOrder = order;
            order++;
        }

   
        
        #region 访问Container

        public T OpenScreen<T>(float fadeTime) where T : Screen
        {
            if (parentContainer != null && parentContainer.isActive)
            {
                return parentContainer.Open<T>(fadeTime);
            }
            return null;
        }

        public TScreen OpenScreen<TScreen, TParam>(TParam param, float fadeTime) where TScreen : Screen<TParam>
        {
            if (parentContainer != null && parentContainer.isActive)
            {
                return parentContainer.Open<TScreen, TParam>(param, fadeTime);
            }
            return null;
        }

        public void Close(float fadeTime)
        {
            parentContainer?.Close(this, fadeTime);
        }

        #endregion

        #region 绑定系统
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

        #endregion

        private void Reset()
        {
            canvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}