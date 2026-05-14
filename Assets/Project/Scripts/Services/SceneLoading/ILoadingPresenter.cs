using Cysharp.Threading.Tasks;

namespace Project.Scripts.Services.SceneLoading
{
    public interface ILoadingPresenter
    {
        UniTask ShowAsync();
        UniTask HideAsync();
        void SetProgress(float progress);
    }
}