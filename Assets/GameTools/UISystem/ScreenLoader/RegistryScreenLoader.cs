using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameTools.UISystem
{
    internal class RegistryScreenLoader: IScreenLoader
    {

        private class ScreenResource
        {
            public bool isUsed =false;
            public readonly ScreenBase screen;

            public ScreenResource(ScreenBase screen)
            {
                this.screen = screen;
            }
        }

        



        private Dictionary<Type,  ScreenResource> screenDict = new();
        public void RegisterScreen(ScreenBase screen) 
        {
            if(screen == null) throw new ArgumentNullException($"{nameof(screen)} must not be null.");
            Type t = screen.GetType();
            if(screenDict.ContainsKey(t)) throw new Exception("Screen already registered");
            screen.SetInit();
            screenDict.Add(t, new ScreenResource(screen));
        }

        public TScreen GetScreen<TScreen>() where TScreen : ScreenBase
        { 
            Type t =typeof(TScreen);
            if (!screenDict.TryGetValue( t, out var resource)) throw new Exception($"Screen: {t} is not registered");
            if(resource.isUsed) throw new Exception($"screen is used");
            resource.isUsed = true;
            return (TScreen)resource.screen;
        }
        
        public void ReleaseScreen(ScreenBase screen)
        {
            Type t = screen.GetType();
            if (!screenDict.TryGetValue(t, out var resource)) throw new Exception($"Screen: {t} is not registered");
            if(resource.screen != screen) throw new Exception($"{screen} and {resource.screen} is not match");
            if(!resource.isUsed) throw new Exception($"screen not be used");
            resource.isUsed = false;
        }
        
        public void Dispose()
        {
            foreach (var resource in screenDict.Values)
            {
                if(resource.isUsed) throw new Exception("Screen is not return");
                resource.screen.SetDispose();
            }
            screenDict.Clear();
        }
    }
}