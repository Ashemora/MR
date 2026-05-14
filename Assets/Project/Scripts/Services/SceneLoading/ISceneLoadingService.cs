using Cysharp.Threading.Tasks;

namespace Project.Scripts.Services.SceneLoading
{
    public interface ISceneLoadingService
    {
        UniTask LoadSceneAsync(string sceneName, ILoadingPresenter loadingPresenter = null,
            int activationDelayMilliseconds = 0);
    }
}