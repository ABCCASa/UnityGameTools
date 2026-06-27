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
    
    [RequireComponent(typeof(Canvas))] [RequireComponent(typeof(CanvasGroup))]
    public abstract class ScreenBase: MonoBehaviour, ILayerItem
    {
        internal ScreenBase() { }
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        private ContainerBase registeredGroup;
        private bool _globalInteractable, _selfInteractable;
       
        private bool globalInteractable
        {
            get => _globalInteractable;
            set 
            {
            _globalInteractable = value;
            canvasGroup.interactable = _globalInteractable && _selfInteractable;
            }
        }

                
        private protected bool selfInteractable 
        {
            get => _selfInteractable;
            set
            {
                _selfInteractable = value;
                canvasGroup.interactable = _globalInteractable && _selfInteractable;
            }
        }
        
        public ScreenState state { get; private set; } = ScreenState.Close;
        public bool isFade { get; private protected set; } = false;
        public abstract bool blockInput { get; }
        public virtual bool enableAutoBind {get;} = true;
        
        protected virtual void OnInit() { }
        protected virtual void OnClose() { }
        protected virtual void OnPause(){ }
        protected virtual void OnResume() { }
        protected virtual void OnDispose() { }

        public void CompleteAnimation()
        {
          if(isFade) UIAnimationManager.Instance.CompleteAnimation(this);
        }

        public void SpeedUpAnimation(float fadeTime)
        {
            if(isFade) UIAnimationManager.Instance.SpeedUpAnimation(this, fadeTime);
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
            if(state!=ScreenState.Open) throw new InvalidOperationException("cannot pause screen when state is not open");
            CompleteAnimation();
            state = ScreenState.Pause;
            selfInteractable = false;
            SafeCall(OnPause);
            isFade = true;
            OpenCloseAnimation(1);
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, false, ResumePauseAnimation, () =>
            {
                isFade = false;
                gameObject.SetActive(false);
                callback?.Invoke();
            });
        }

        internal void SetResume(float fadeTime = -1, Action callback = null)
        {
            if(state!=ScreenState.Pause) throw new InvalidOperationException("cannot resume screen when state is not pause");
            CompleteAnimation();
            state = ScreenState.Open;
            gameObject.SetActive(true);
            SafeCall(OnResume);
            isFade = true;
            OpenCloseAnimation(1);
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, true, ResumePauseAnimation, () =>
            {
                isFade = false;
                selfInteractable = true;
                callback?.Invoke();
            });
        }

        internal void SetClose(float fadeTime = -1, Action callback = null)
        {
            if (state == ScreenState.Close) throw new InvalidOperationException("cannot close screen when state is close");
            if (state == ScreenState.Pause) fadeTime = -1;
            CompleteAnimation();
            registeredGroup = null;
            state = ScreenState.Close;
            selfInteractable = false;
            SafeCall(OnClose);   
            isFade = true;
            ResumePauseAnimation(1);
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, false, OpenCloseAnimation, () =>
            {
                isFade = false;
                gameObject.SetActive(false);
                callback?.Invoke();
            });
        }
        
        private protected void SetOpen(Action onOpen, float fadeTime, ContainerBase group)
        { 
            if(state!= ScreenState.Close)  throw new InvalidOperationException("cannot open screen when state is not close");
            CompleteAnimation();
            state = ScreenState.Open;
            gameObject.SetActive(true);
            SafeCall(onOpen);
            registeredGroup = group;
            isFade = true;
            ResumePauseAnimation(1);
            UIAnimationManager.Instance.SetAnimation(this, fadeTime, true, OpenCloseAnimation, () =>
            {
                isFade = false;
                selfInteractable = true;
            });
        }
        
        protected virtual void OpenCloseAnimation(float progress)
        {
            canvasGroup.alpha = progress;
        }

        protected virtual void ResumePauseAnimation(float progress)
        {
            canvasGroup.alpha = 1;
        }
       
        void ILayerItem.SetInteractable(bool value) => globalInteractable = value;

        void ILayerItem.SetOrder(int order) => canvas.sortingOrder = order;

        internal void SafeCall(Action action)
        {
            try { action?.Invoke(); }
            catch (Exception e) {  Debug.LogException( e,this); }
        }

        
        #region 访问Container
        public T OpenScreen<T>(float fadeTime) where T : Screen
        {
            if (registeredGroup != null && registeredGroup.isActive)
            {
                return registeredGroup.Open<T>(fadeTime);
            }
            return null;
        }
        
        public TScreen OpenScreen<TScreen, TParam>(TParam param,float fadeTime) where TScreen : Screen<TParam>
        {
            if (registeredGroup != null && registeredGroup.isActive)
            {
                return registeredGroup.Open<TScreen, TParam>(param, fadeTime);
            }
            return null;
        }

        public void Close(float fadeTime)
        {
            registeredGroup?.Close(this, fadeTime);
        }

        #endregion

        #region 绑定系统
        protected T Find<T>(string path = null) where T : Component
        {
            Transform trans = Find(path);
            if(trans == null) return null;
            T target = trans.GetComponent<T>();
            return target;
        }
        
        protected Transform Find(string path = null)
        {
            if (string.IsNullOrEmpty(path)) return transform;
            return transform.Find(path); 
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
                        GameObject trans = Find(path).gameObject;
                        var bind1 = bind;
                        RegisterBindAction(() =>
                            {
                              var(find, process)  = bind1.Bind(trans.gameObject);
                              if (find == 0) { throw new Exception($"fail to bind with {path}"); }
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
                        else { throw new Exception($"Method: {method} 不支持自动绑定"); }
                    }
                    else // > 1
                    {
                        throw new Exception($"Method: {method} 不支持自动绑定");
                    }
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