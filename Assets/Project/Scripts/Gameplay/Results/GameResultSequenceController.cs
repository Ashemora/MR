using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.UI;
using Project.Scripts.Services.Announcements;
using Project.Scripts.Services.Events;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Gameplay.Results
{
    public class GameResultSequenceController : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly GameResultSequenceConfig _config;
        private readonly GameResultPresenter _gameResultPresenter;
        private readonly IBoardAnnouncementService _announcementService;
        private readonly CancellationTokenSource _disposeCts = new();
        private IDisposable _resultSub;
        private IGameResultVisuals _visuals;
        private bool _isRunning;


        public GameResultSequenceController(EventBus eventBus, GameResultSequenceConfig config,
            GameResultPresenter gameResultPresenter, IBoardAnnouncementService announcementService)
        {
            _eventBus = eventBus;
            _config = config;
            _gameResultPresenter = gameResultPresenter;
            _announcementService = announcementService;
        }


        public void Initialize()
        {
            _resultSub ??= _eventBus.Subscribe<GameResultEvent>(OnGameResult);
        }

        public void BindVisuals(IGameResultVisuals visuals)
        {
            _visuals = visuals;
        }

        public void Dispose()
        {
            _resultSub?.Dispose();
            _resultSub = null;
            _disposeCts.Cancel();
            _disposeCts.Dispose();
        }


        private void OnGameResult(GameResultEvent e)
        {
            if (_isRunning)
                return;

            _isRunning = true;
            RunSequence(e).Forget();
        }

        private async UniTaskVoid RunSequence(GameResultEvent e)
        {
            try
            {
                if (e.Winner == BattleSide.Player)
                    await RunWinSequence(e, _disposeCts.Token);
                else
                    await RunLoseSequence(_disposeCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask RunWinSequence(GameResultEvent e, CancellationToken cancellationToken)
        {
            var sequence = _config.WinSequence;
            await DelayIfNeeded(sequence.DelayBeforeFirstStep, cancellationToken);

            if (sequence.WinnerAvatarPulse.Enabled)
            {
                if (null != _visuals)
                    await _visuals.PlayAvatarPulse(e.Winner, sequence.WinnerAvatarPulse);

                await DelayIfNeeded(sequence.WinnerAvatarPulse.DelayAfterStep, cancellationToken);
            }

            if (e.IsFlawless && sequence.ShowFlawlessAnnouncement.Enabled)
            {
                await _announcementService.Show("Flawless Victory!");
                await DelayIfNeeded(sequence.ShowFlawlessAnnouncement.DelayAfterStep, cancellationToken);
            }

            if (sequence.ShowWinPopup.Enabled)
            {
                await _gameResultPresenter.ShowWin(e.IsFlawless);
                await DelayIfNeeded(sequence.ShowWinPopup.DelayAfterStep, cancellationToken);
            }
        }

        private async UniTask RunLoseSequence(CancellationToken cancellationToken)
        {
            var sequence = _config.LoseSequence;
            await DelayIfNeeded(sequence.DelayBeforeFirstStep, cancellationToken);

            if (sequence.ShowLosePopup.Enabled)
            {
                await _gameResultPresenter.ShowLose();
                await DelayIfNeeded(sequence.ShowLosePopup.DelayAfterStep, cancellationToken);
            }
        }

        private static async UniTask DelayIfNeeded(float seconds, CancellationToken cancellationToken)
        {
            if (seconds <= 0f)
                return;

            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken);
        }
    }
}