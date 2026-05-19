using Cysharp.Threading.Tasks;
using Project.Scripts.Services.SceneLoading;
using R3;

namespace Project.Scripts.Services.AppFlow
{
    public interface IAppStateMachine
    {
        ReadOnlyReactiveProperty<AppState> State { get; }
        UniTask EnterLobbyAsync(ILoadingPresenter loadingPresenter = null, int activationDelayMilliseconds = 0);
        UniTask StartBattleAsync();
        UniTask ReturnToLobbyAsync();
    }
}