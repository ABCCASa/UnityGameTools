using System;
using UnityEngine;
using UnityEngine.Assertions;


namespace GameTools.UISystem
{
    public class TransitionHandler
    {
        public ScreenState state;
        public float fadeTime;
        public TransitionHandler(ScreenState state, float fadeTime)
        {
            this.state = state;
            this.fadeTime = fadeTime;
        }
    }

    public class TransitionVersion
    {
        private int version = 0;
        public int GetVersion()
        {
            version++;
            return version;;
        }
        public bool ValidVersion(int version)
        {
            return this.version == version;
        }
    }


    public enum ScreenState { Uninitialize, Open, Close, Pause, Dispose }
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
        public ScreenState state { get; private set; } = ScreenState.Uninitialize;
        private IAnimationHandler _animationHandler;

        private IAnimationHandler animationHandler
        {
            get
            {
                if (_animationHandler != null && _animationHandler.isComplete) _animationHandler = null;
                return _animationHandler;
            }
            set
            {
                Assert.IsTrue(_animationHandler?.isComplete ?? true); // 动画被替换时已经完成
                if (value != null && value.isComplete) _animationHandler = null;
                else _animationHandler = value;
            }
        }
        private readonly TransitionVersion transitionVersion = new();
        public bool isFade => animationHandler != null;
        public abstract bool blockInput { get; }

        protected virtual void OnInit() { }
        protected virtual void OnClose() { }
        protected virtual void OnPause() { }
        protected virtual void OnResume() { }
        protected virtual void OnDispose() { }

        public void CompleteAnimation()
        {
            animationHandler?.CompleteAnimation();
        }
        
        public void SpeedUpAnimation(float fadeTime)
        {
            animationHandler?.SpeedUpAnimation(fadeTime);
        }

        internal void SetInit()
        {  
            if (state != ScreenState.Uninitialize) throw new InvalidOperationException("cannot dispose a screen when state is not close");
            state = ScreenState.Close;
            selfInteractable = false;
            globalInteractable = false;
            gameObject.SetActive(false); // 默认关闭，所以先要隐藏
            SafeCall(OnInit);
        }

        internal void SetDispose()
        {
            CompleteAnimation();
            if (state != ScreenState.Close) throw new InvalidOperationException("cannot dispose a screen when state is not close");
            state = ScreenState.Dispose;
            SafeCall(OnDispose);
            Destroy(gameObject);
        }

        private protected void SetOpen(Action onOpen, float fadeTime, ScreenContainer container, string animKey = null)
        {
            CompleteAnimation();
            if (state != ScreenState.Close) throw new InvalidOperationException("cannot open screen when state is not close");
            int version = transitionVersion.GetVersion();
            state = ScreenState.Open;
            gameObject.SetActive(true);
            parentContainer = container; 
            animationHandler = UIAnimationManager.Instance.SetAnimation( fadeTime, true, (progress) => Animation(animKey, progress));
            SafeCall(onOpen);
            if(!transitionVersion.ValidVersion(version)) return; 
            if (isFade)animationHandler.AddCallBack( () => selfInteractable = true);
            else selfInteractable = true;
        }
        
        internal void SetClose(float fadeTime = -1, string animKey = null, Action callback = null)
        {
            CompleteAnimation();
            if (state != ScreenState.Open || state != ScreenState.Pause) throw new InvalidOperationException("cannot close screen when state is close");
            int version = transitionVersion.GetVersion();
            ScreenState previousState = state;
            state = ScreenState.Close;
            selfInteractable = false;
            if (previousState == ScreenState.Open)
            {
               animationHandler = UIAnimationManager.Instance.SetAnimation(fadeTime, false, (progress) => Animation(animKey, progress));
            }
            SafeCall(OnClose);
            if(!transitionVersion.ValidVersion(version)) return;
            parentContainer = null;
            if (isFade)
            {
                animationHandler.AddCallBack(() =>
                {
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
            CompleteAnimation();
            if (state != ScreenState.Open) throw new InvalidOperationException("cannot pause screen when state is not open");
            int version = transitionVersion.GetVersion();
            state = ScreenState.Pause;
            selfInteractable = false;
            animationHandler = UIAnimationManager.Instance.SetAnimation(fadeTime, false, (progress) => Animation(animKey, progress));
            SafeCall(OnPause);
            if(!transitionVersion.ValidVersion(version)) return; 
            if (isFade)
            {
                animationHandler.AddCallBack(() =>
                {
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

        internal void SetResume(float fadeTime = -1, string animKey = null)
        {
            CompleteAnimation();
            if (state != ScreenState.Pause) throw new InvalidOperationException("cannot resume screen when state is not pause");
            int version = transitionVersion.GetVersion();
            state = ScreenState.Open;
            gameObject.SetActive(true);
            animationHandler = UIAnimationManager.Instance.SetAnimation(fadeTime, true, (progress) => Animation(animKey, progress));
            SafeCall(OnResume);
            if(!transitionVersion.ValidVersion(version)) return; 
            if (isFade) animationHandler.AddCallBack(() => selfInteractable = true);
            else selfInteractable = true;
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
    }
}