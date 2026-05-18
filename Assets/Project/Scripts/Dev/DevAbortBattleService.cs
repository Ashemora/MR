#if DEV
using Cysharp.Threading.Tasks;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.Services.BattleFlow;
using Project.Scripts.Services.Game;

namespace Project.Scripts.Dev
{
    public class DevAbortBattleService
    {
        private readonly IAppStateMachine _appStateMachine;
        private readonly IBattleActionRuntimeService _battleActionRuntimeService;
        private readonly IBattleFlowService _battleFlowService;

        
        private bool _isAborting;


        public DevAbortBattleService(IAppStateMachine appStateMachine,
            IBattleActionRuntimeService battleActionRuntimeService, IBattleFlowService battleFlowService)
        {
            _appStateMachine = appStateMachine;
            _battleActionRuntimeService = battleActionRuntimeService;
            _battleFlowService = battleFlowService;
        }


        public bool CanAbort()
        {
            return false == _isAborting;
        }

        public void TryAbort()
        {
            if (_isAborting)
                return;

            _isAborting = true;
            _battleActionRuntimeService.MarkBlocked();

            if (_battleFlowService.IsInitialized)
                _battleFlowService.MarkFinished();

            AbortAsync().Forget();
        }


        private async UniTaskVoid AbortAsync()
        {
            await _appStateMachine.ReturnToLobbyAsync();
        }
    }
}
#endif