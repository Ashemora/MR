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
    public class WinViewModel : BaseViewModel
    {
        public int MovesUsed { get; private set; }
        public int LevelId { get; private set; }
        public string OpponentName { get; private set; }
        public bool IsFlawless { get; }


        private readonly IMoveCounterService _moveCounter;
        private readonly ILevelProgressionService _progression;
        private readonly Action _onClose;


        public WinViewModel(
            IMoveCounterService moveCounter,
            ILevelProgressionService progression,
            int levelId,
            string opponentName,
            bool isFlawless,
            Action onClose)
        {
            _moveCounter = moveCounter;
            _progression = progression;
            LevelId = levelId;
            OpponentName = opponentName;
            IsFlawless = isFlawless;
            _onClose = onClose;
        }

        public void NextLevel()
        {
            _onClose?.Invoke();
            _progression.Advance();
        }


        protected override UniTask OnInitializeAsync()
        {
            MovesUsed = _moveCounter.MovesUsed;
            return UniTask.CompletedTask;
        }
    }
}