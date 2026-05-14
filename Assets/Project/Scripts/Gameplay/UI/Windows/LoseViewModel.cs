using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.AppFlow;
using Project.Scripts.Services.UISystem;

namespace Project.Scripts.Gameplay.UI.Windows
{
    public class LoseViewModel : BaseViewModel
    {
        public int MovesUsed { get; private set; }
        public int LevelId { get; }
        public string OpponentName { get; }


        private readonly IMoveCounterService _moveCounter;
        private readonly IAppStateMachine _appStateMachine;
        private readonly Action _onClose;


        public LoseViewModel(IMoveCounterService moveCounter, IAppStateMachine appStateMachine,
            int levelId, string opponentName, Action onClose)
        {
            _moveCounter = moveCounter;
            _appStateMachine = appStateMachine;
            LevelId = levelId;
            OpponentName = opponentName;
            _onClose = onClose;
        }


        public void Retry()
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