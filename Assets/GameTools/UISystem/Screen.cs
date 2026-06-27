using System;

namespace GameTools.UISystem
{
    public abstract class Screen: ScreenBase
    {
        internal void SetOpen(float fadeTime, ContainerBase group)
        { 
           SetOpen(()=> SafeCall(OnOpen) , fadeTime, group);
        }
        protected virtual void OnOpen() { }
    }
    
    public abstract class Screen<T>:ScreenBase
    {
        internal void SetOpen(T param, float fadeTime, ContainerBase group)
        {
            SetOpen(() => SafeCall(() => OnOpen(param)) , fadeTime, group);
        }
        protected virtual void OnOpen(T param) { }
    }
}