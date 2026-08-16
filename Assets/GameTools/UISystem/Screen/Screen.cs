using System;
using UnityEngine;

namespace GameTools.UISystem
{
    public abstract class Screen: ScreenBase 
    {
        internal void SetOpen(float fadeTime, ScreenContainer container, string animKey=null, Action callback=null) => SetOpen(OnOpen, fadeTime, container, animKey, callback);
        protected virtual void OnOpen() { }
    }
    
    public abstract class Screen<TParam>: ScreenBase 
    {
        internal void SetOpen(TParam param, float fadeTime, ScreenContainer container, string animKey=null,  Action callback=null) => SetOpen(() => OnOpen(param), fadeTime, container, animKey, callback);
        protected virtual void OnOpen(TParam param) { }
    }
}