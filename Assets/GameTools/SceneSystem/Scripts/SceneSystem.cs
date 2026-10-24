
using GameTools.Singletons;
using GameTools.UISystem;
using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameTools.SceneSystem
{
    public class SceneSystem : MonoLazySingleton<SceneSystem>
    {
        private static ScreenContainer container = LayerManager.AddContainer<ScreenContainer>(1000);
        public static bool isBusy { get; private set; } = false;
        public static event Action onSceneLoaded, onSceneUnloaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (SceneBase.Instance == null) { return; }
            isBusy = true;
            
            var screen = container.Open<DarkTransitionScreen>();
            IEnumerator FirstSceneInit() 
            {
                try 
                {   
                    yield return SceneBase.Instance.OpenScene();
                    onSceneLoaded?.Invoke();
                    screen.Close(0.2f);
                }
                finally { isBusy = false; }
            }
           screen.StartCoroutine(FirstSceneInit());
        }

        private static IEnumerator LoadSceneAsync(UGUIScreen screen, string sceneName, float minLoadingTime, float fadeOutTime)
        {
            try
            {
                while (screen.isFade)
                {
                    yield return null;
                }
                //开始异步加载场景
                var op = SceneManager.LoadSceneAsync(sceneName);
                if (op == null) {  yield break; }

                op.allowSceneActivation = false;
                float startTime = Time.unscaledTime;

                //卸载场景(也就是加载ui完全展示时)
                onSceneUnloaded?.Invoke();
                if (SceneBase.Instance != null)
                {
                    yield return SceneBase.Instance.CloseScene();
                }

                //等待
                while (op.progress < 0.9f)
                {
                    yield return null;
                }

                float timeUsage = Time.unscaledTime - startTime;
                if (timeUsage < minLoadingTime) yield return new WaitForSecondsRealtime(minLoadingTime - timeUsage);

                op.allowSceneActivation = true;
                while (!op.isDone) { yield return null; }

                //初始化新场景
                if (SceneBase.Instance != null) { yield return SceneBase.Instance.OpenScene(); }
                onSceneLoaded?.Invoke();
            }
            finally
            {
                isBusy = false;
                screen.Close(fadeOutTime);   
            }
        }
        
        private static IEnumerator LoadSceneAsync<TSceneParam>(UGUIScreen screen, string sceneName, TSceneParam param, float minLoadingTime, float fadeOutTime)
        {
            try
            {
                while (screen.isFade)
                {
                    yield return null;
                }
                
                //开始异步加载场景
                var op = SceneManager.LoadSceneAsync(sceneName);
                if (op == null)
                {
                    yield break;
                }

                op.allowSceneActivation = false;
                float startTime = Time.unscaledTime;

                //卸载场景(也就是ui完全展示时)
                onSceneUnloaded?.Invoke();
                if (SceneBase.Instance != null) yield return SceneBase.Instance.CloseScene();

                //等待
                while (op.progress < 0.9f)
                {
                    yield return null;
                }

                float timeUsage = Time.unscaledTime - startTime;
                if (timeUsage < minLoadingTime) yield return new WaitForSecondsRealtime(minLoadingTime - timeUsage);

                op.allowSceneActivation = true;
                while (!op.isDone)
                {
                    yield return null;
                }

                //初始化新场景
                if (SceneBase.Instance == null) throw new Exception($"场景 {sceneName} 没有带有 SceneBase，无法传入参数");
                yield return SceneBase.Instance.OpenScene(param);
                onSceneLoaded?.Invoke();
            }
            finally
            {
                isBusy = false;
                screen.Close(fadeOutTime);
            }
        }

        public static bool LoadScene<SceneParam>(string sceneName, SceneParam param, float fadeInTime = 0.2f, float fadeOutTime = 0.2f, float minLoadingTime =0.2f)
        {
          return LoadScene<SceneParam, DarkTransitionScreen>(sceneName, param, fadeInTime, fadeOutTime, minLoadingTime);
        }

        public static bool LoadScene(string sceneName, float fadeInTime = 0.2f, float fadeOutTime = 0.2f, float minLoadingTime =0.2f)
        {
            return LoadScene<DarkTransitionScreen >(sceneName, fadeInTime, fadeOutTime, minLoadingTime);
        }

        
        public static bool LoadScene<TFadeScreen>(string sceneName, float fadeInTime = 0.2f, float fadeOutTime = 0.2f, float minLoadingTime =0.2f) where TFadeScreen: UGUIScreen
        {
            if (isBusy) return false;
            isBusy = true;
            try
            {
                var screen = container.Open<TFadeScreen>(fadeInTime);
                screen.StartCoroutine(LoadSceneAsync(screen, sceneName, minLoadingTime, fadeOutTime));
            }
            catch (Exception e)
            {
                isBusy = false;
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return true;
        }

      
        
        public static bool LoadScene<SceneParam, TFadeScreen>(string sceneName, SceneParam sceneParam, float fadeInTime = 0.2f, float fadeOutTime = 0.2f, float minLoadingTime =0.2f) where TFadeScreen : UGUIScreen
        {
            if (isBusy) return false;
            isBusy = true;
            try
            {
                var screen = container.Open<TFadeScreen>(fadeInTime);
                screen.StartCoroutine(LoadSceneAsync(screen, sceneName, sceneParam, minLoadingTime, fadeOutTime));
            }
            catch (Exception e)
            {
                isBusy = false;
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return true;
        }

        
        /*
        public static bool LoadScene<TFadeScreen, TFadeParam>(string sceneName, TFadeParam fadeParam, float fadeInTime = 0.2f, float fadeOutTime = 0.2f, float minLoadingTime =0.2f) where TFadeScreen : FadeScreen<TFadeParam>
        {
            if (isBusy) return false;
            isBusy = true;
            try
            {
                IEnumerator enumerator = LoadSceneAsync(sceneName, minLoadingTime);
                UIManager.OpenScreen<TFadeScreen,(TFadeParam,IEnumerator)>(10000, (fadeParam, enumerator), fadeInTime, fadeOutTime);
            }
            catch (Exception e)
            {
                isBusy = false;
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return true;
        }
        public static bool LoadScene<SceneParam, TFadeScreen, TFadeParam>(string sceneName, SceneParam sceneParam,TFadeParam fadeParam, float fadeInTime = 0.2f, float fadeOutTime = 0.2f, float minLoadingTime =0.2f) where TFadeScreen : FadeScreen<TFadeParam>
        {
            if (isBusy) return false;
            isBusy = true;
            try
            {
                IEnumerator enumerator = LoadSceneAsync(sceneName, sceneParam, minLoadingTime);
                UIManager.OpenScreen<TFadeScreen, (TFadeParam, IEnumerator)>(10000, (fadeParam, enumerator), fadeInTime, fadeOutTime);
            }
            catch (Exception e)
            {
                isBusy = false;
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return true;
        }*/
    }
}