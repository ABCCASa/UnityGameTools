using System;

namespace GameTools.UISystem
{
    public abstract class Screen: ScreenBase
    {
        internal void SetOpen(float fadeTime, ContainerBase group, Action callback=null) => SetOpen(OnOpen, fadeTime, group, callback);
        protected virtual void OnOpen() { }
    }
    
    public abstract class Screen<TParam>:ScreenBase
    {
        internal void SetOpen(TParam param, float fadeTime, ContainerBase group,  Action callback=null) => SetOpen(() => OnOpen(param), fadeTime, group, callback);
        protected virtual void OnOpen(TParam param) { }
    }
}