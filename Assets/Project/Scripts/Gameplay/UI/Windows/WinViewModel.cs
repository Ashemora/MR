using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.Services.UISystem;

namespace Project.Scripts.Gameplay.UI.Windows
{
    public class WinViewModel : BaseViewModel
    {
        public int MovesUsed { get; private set; }
        public string OpponentName { get; private set; }
        public bool IsFlawless { get; }


        private readonly IMoveCounterService _moveCounter;
        private readonly IAppStateMachine _appStateMachine;
        private readonly Action _onClose;


        public WinViewModel(IMoveCounterService moveCounter, IAppStateMachine appStateMachine,
            string opponentName, bool isFlawless, Action onClose)
        {
            _moveCounter = moveCounter;
            _appStateMachine = appStateMachine;
            OpponentName = opponentName;
            IsFlawless = isFlawless;
            _onClose = onClose;
        }

        public void NextLevel()
        {
            _onClose?.Invoke();
            _appStateMachine.ReturnToLobbyAsync().Forget();
        }


        protected override UniTask OnInitializeAsync()
        {
            MovesUsed = _moveCounter.MovesUsed;
            return UniTask.CompletedTask;
        }
    }
}