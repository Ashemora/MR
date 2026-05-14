using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Scripts.Services.SceneLoading
{
    public class SceneLoadingService : ISceneLoadingService
    {
        private const int InterStagePauseMs = 80;

        private static readonly LoadingStage[] Stages =
        {
            new(weight: 0.20f, minDurationMs: 250),
            new(weight: 0.35f, minDurationMs: 450),
            new(weight: 0.25f, minDurationMs: 350),
            new(weight: 0.20f, minDurationMs: 350),
        };

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

            var cumulative = 0f;
            for (var i = 0; i < Stages.Length; i++)
            {
                var stage = Stages[i];
                await RunStage(loadingPresenter, cumulative, cumulative + stage.Weight, stage.MinDurationMs);
                cumulative += stage.Weight;

                if (i < Stages.Length - 1)
                    await UniTask.Delay(InterStagePauseMs, ignoreTimeScale: true);
            }

            while (asyncOp.progress < 0.9f)
                await UniTask.Yield();

            if (activationDelayMilliseconds > 0)
                await UniTask.Delay(activationDelayMilliseconds, ignoreTimeScale: true);

            loadingPresenter?.SetProgress(1f);
            asyncOp.allowSceneActivation = true;

            while (false == asyncOp.isDone)
                await UniTask.Yield();

            if (null != loadingPresenter)
                await loadingPresenter.HideAsync();
        }

        private static async UniTask RunStage(ILoadingPresenter loadingPresenter, float fromProgress, float toProgress, int durationMs)
        {
            var duration = durationMs / 1000f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 2f);
                loadingPresenter?.SetProgress(Mathf.Lerp(fromProgress, toProgress, eased));
                await UniTask.Yield();
            }

            loadingPresenter?.SetProgress(toProgress);
        }

        private readonly struct LoadingStage
        {
            public readonly float Weight;
            public readonly int MinDurationMs;

            public LoadingStage(float weight, int minDurationMs)
            {
                Weight = weight;
                MinDurationMs = minDurationMs;
            }
        }
    }
}