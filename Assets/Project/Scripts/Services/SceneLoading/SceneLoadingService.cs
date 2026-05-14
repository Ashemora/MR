using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Project.Scripts.Services.SceneLoading
{
    public class SceneLoadingService : ISceneLoadingService
    {
        public async UniTask LoadSceneAsync(string sceneName, ILoadingPresenter loadingPresenter = null,
            int activationDelayMilliseconds = 0)
        {
            if (null != loadingPresenter)
                await loadingPresenter.ShowAsync();

            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (null == asyncOp)
                throw new InvalidOperationException($"Failed to load scene: {sceneName}. Make sure it is added to Build Settings.");

            asyncOp.allowSceneActivation = false;
            loadingPresenter?.SetProgress(0f);

            while (asyncOp.progress < 0.9f)
            {
                loadingPresenter?.SetProgress(asyncOp.progress / 0.9f);
                await UniTask.Yield();
            }

            loadingPresenter?.SetProgress(1f);
            if (activationDelayMilliseconds > 0)
                await UniTask.Delay(activationDelayMilliseconds);

            if (null != loadingPresenter)
                await loadingPresenter.HideAsync();

            asyncOp.allowSceneActivation = true;

            while (false == asyncOp.isDone)
                await UniTask.Yield();
        }
    }
}