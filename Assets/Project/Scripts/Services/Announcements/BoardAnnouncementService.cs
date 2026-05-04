using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.UI;
using Project.Scripts.Gameplay.UI.Windows;
using Project.Scripts.Services.Board;
using Project.Scripts.Services.UISystem;
using R3;
using VContainer.Unity;

namespace Project.Scripts.Services.Announcements
{
    public class BoardAnnouncementService : IBoardAnnouncementService, IStartable, IDisposable
    {
        private const int InitialPoolSize = 2;

        
        public ReadOnlyReactiveProperty<bool> IsEnergyTextHidden => _isEnergyTextHidden;
        

        private readonly BoardAnnouncementConfig _config;
        private readonly IBoardBoundsProvider _boardBounds;
        private readonly UIService _uiService;
        private readonly Queue<BoardAnnouncementView> _pool = new();
        private readonly List<BoardAnnouncementView> _all = new();
        private readonly ReactiveProperty<bool> _isEnergyTextHidden = new(false);

        
        public BoardAnnouncementService(BoardAnnouncementConfig config, IBoardBoundsProvider boardBounds,
            UIService uiService)
        {
            _config = config;
            _boardBounds = boardBounds;
            _uiService = uiService;
        }

        public void Start()
        {
            if (false == _config.ViewPrefab)
                return;

            for (var i = 0; i < InitialPoolSize; i++)
                _pool.Enqueue(CreateInstance());
        }

        public async UniTask Show(string text, BoardAnnouncementParams @params = null)
        {
            if (false == _config.ViewPrefab)
                return;

            var hideEnergyText = @params?.HideEnergyText ?? _config.HideEnergyText;

            if (hideEnergyText)
                _isEnergyTextHidden.Value = true;

            var view = GetOrCreate();
            var vm = BuildViewModel(text, @params);

            await view.InitializeAsync(vm);
            await view.ShowAsync();
            await view.HideAsync();

            _pool.Enqueue(view);

            if (hideEnergyText)
                _isEnergyTextHidden.Value = false;
        }

        public void Dispose()
        {
            _isEnergyTextHidden.Dispose();

            for (var i = 0; i < _all.Count; i++)
            {
                if (_all[i])
                    UnityEngine.Object.Destroy(_all[i].gameObject);
            }

            _all.Clear();
            _pool.Clear();
        }


        private BoardAnnouncementViewModel BuildViewModel(string text, BoardAnnouncementParams @params)
        {
            var style = @params?.Style ?? _config.Style;
            var anchor = @params?.Anchor ?? _config.DefaultAnchor;
            var textColor = @params?.TextColor ?? _config.TextColor;
            var displayDuration = @params?.DisplayDuration ?? _config.DisplayDuration;
            var fadeOutDuration = @params?.FadeOutDuration ?? _config.FadeOutDuration;
            var flyDistance = @params?.FlyDistance ?? _config.FlyDistance;
            var baseScale = @params?.BaseScale ?? 1f;
            var scaleMultiplier = @params?.ScaleMultiplier ?? _config.ScaleMultiplier;
            var fadeOutEase = @params?.FadeOutEase ?? _config.FadeOutEase;
            var worldY = GetAnchorWorldY(anchor) + _config.VerticalWorldOffset;
            float WorldYGetter() => GetAnchorWorldY(anchor) + _config.VerticalWorldOffset;

            return new BoardAnnouncementViewModel(
                style,
                text,
                textColor,
                displayDuration,
                fadeOutDuration,
                flyDistance,
                baseScale,
                scaleMultiplier,
                fadeOutEase,
                worldY,
                WorldYGetter);
        }

        private float GetAnchorWorldY(AnnouncementAnchorKind anchor)
        {
            return anchor switch
            {
                AnnouncementAnchorKind.EnergyBars => _boardBounds.EnergyBarsAnchorWorldY,
                AnnouncementAnchorKind.Board => _boardBounds.BoardAnchorWorldY,
                _ => _boardBounds.BattleFieldAnchorWorldY
            };
        }

        private BoardAnnouncementView GetOrCreate()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            return CreateInstance();
        }

        private BoardAnnouncementView CreateInstance()
        {
            var parent = _uiService.GetLayerRoot(UILayer.Popup);
            var go = UnityEngine.Object.Instantiate(_config.ViewPrefab, parent);
            var view = go.GetComponent<BoardAnnouncementView>();
            go.SetActive(false);
            _all.Add(view);

            return view;
        }
    }
}