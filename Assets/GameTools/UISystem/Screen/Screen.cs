namespace GameTools.UISystem
{
    public abstract class UGUIScreen: UGUIScreenBase 
    {
        internal void SetOpen(float fadeTime, ScreenContainer container, string animKey=null) => SetOpen(OnOpen, fadeTime, container, animKey);
        protected virtual void OnOpen() { }
    }
    
    public abstract class UGUIScreen<TParam>: UGUIScreenBase 
    {
        internal void SetOpen(TParam param, float fadeTime, ScreenContainer container, string animKey=null) => SetOpen(() => OnOpen(param), fadeTime, container, animKey);
        protected abstract void OnOpen(TParam param);
    }
}