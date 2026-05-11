using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.UI;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.Combat.Moves;
using Project.Scripts.Services.Events;
using Project.Scripts.Services.UISystem;
using R3;

namespace Project.Scripts.Gameplay.UI
{
    public class MoveBarViewModel : BaseViewModel
    {
        private readonly EventBus _eventBus;
        private readonly MoveBarConfig _config;
        private readonly IMoveBarService _moveBarService;
        private readonly IBoardBoundsProvider _boardBounds;
        private readonly Subject<Unit> _swapRejectedSubject = new();


        public ReactiveProperty<int> CurrentMoves { get; } = new(0);
        public ReactiveProperty<float> FillProgress { get; } = new(0f);
        public ReactiveProperty<bool> IsAtMax { get; } = new(false);
        public int MaxMoves { get; private set; }
        public MoveBarConfig Config => _config;
        public Observable<Unit> OnSwapRejected => _swapRejectedSubject;
        public float BoardHalfWidth => _boardBounds.BoardHalfWidth;
        public float BoardCenterX => _boardBounds.BoardCenterX;


        public MoveBarViewModel(EventBus eventBus, MoveBarConfig config, IMoveBarService moveBarService, IBoardBoundsProvider boardBounds)
        {
            _eventBus = eventBus;
            _config = config;
            _moveBarService = moveBarService;
            _boardBounds = boardBounds;
        }


        protected override UniTask OnInitializeAsync()
        {
            MaxMoves = _config.MaxMoves;

            var snapshot = _moveBarService.GetSnapshot();
            CurrentMoves.Value = snapshot.CurrentMoves;
            FillProgress.Value = snapshot.FillProgress;
            IsAtMax.Value = snapshot.IsAtMax;

            Disposables.Add(_eventBus.Subscribe<MoveBarChangedEvent>(OnMoveBarChanged));
            Disposables.Add(_eventBus.Subscribe<SwapRejectedEvent>(_ => _swapRejectedSubject.OnNext(Unit.Default)));

            return UniTask.CompletedTask;
        }

        protected override void OnCleanup()
        {
            CurrentMoves.Dispose();
            FillProgress.Dispose();
            IsAtMax.Dispose();
            _swapRejectedSubject.Dispose();
        }


        private void OnMoveBarChanged(MoveBarChangedEvent e)
        {
            CurrentMoves.Value = e.CurrentMoves;
            FillProgress.Value = e.FillProgress;
            IsAtMax.Value = e.IsAtMax;
        }
    }
}