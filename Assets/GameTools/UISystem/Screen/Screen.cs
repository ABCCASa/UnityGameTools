using System;
using UnityEngine;

namespace GameTools.UISystem
{
    public abstract class Screen: ScreenBase 
    {
        internal void SetOpen(float fadeTime, ScreenContainer container, string animKey=null) => SetOpen(OnOpen, fadeTime, container, animKey);
        protected virtual void OnOpen() { }
    }
    
    public abstract class Screen<TParam>: ScreenBase 
    {
        internal void SetOpen(TParam param, float fadeTime, ScreenContainer container, string animKey=null) => SetOpen(() => OnOpen(param), fadeTime, container, animKey);
        protected virtual void OnOpen(TParam param) { }
    }
}