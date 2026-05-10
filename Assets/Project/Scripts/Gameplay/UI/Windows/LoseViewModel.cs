using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Services.Combat.Abilities;
using Project.Scripts.Services.Combat.Buffs;
using Project.Scripts.Services.Combat.Passives;
using Project.Scripts.Services.Combat.Units;
using Project.Scripts.Services.Combat.Energy;
using Project.Scripts.Services.Combat.Economy;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Progression;
using Project.Scripts.Services.UISystem;

namespace Project.Scripts.Gameplay.UI
{
    public class LoseViewModel : BaseViewModel
    {
        public int MovesUsed { get; private set; }
        public int LevelId { get; }
        public string OpponentName { get; }


        private readonly IMoveCounterService _moveCounter;
        private readonly ILevelProgressionService _progression;
        private readonly Action _onClose;


        public LoseViewModel(
            IMoveCounterService moveCounter,
            ILevelProgressionService progression,
            int levelId,
            string opponentName,
            Action onClose)
        {
            _moveCounter = moveCounter;
            _progression = progression;
            LevelId = levelId;
            OpponentName = opponentName;
            _onClose = onClose;
        }


        public void Retry()
        {
            _onClose?.Invoke();
            _progression.Retry();
        }


        protected override UniTask OnInitializeAsync()
        {
            MovesUsed = _moveCounter.MovesUsed;
            return UniTask.CompletedTask;
        }
    }
}
