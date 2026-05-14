using Cysharp.Threading.Tasks;
using Project.Scripts.Services.SceneLoading;

namespace Project.Scripts.Services.AppFlow
{
    public interface IAppStateMachine
    {
        AppState Current { get; }
        UniTask EnterLobbyAsync(ILoadingPresenter loadingPresenter = null, int activationDelayMilliseconds = 0);
        UniTask StartBattleAsync();
        UniTask ReturnToLobbyAsync();
    }
}