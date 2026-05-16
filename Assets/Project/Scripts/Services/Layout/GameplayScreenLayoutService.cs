using Project.Scripts.Configs.Gameplay;
using Project.Scripts.Gameplay.Layout;
using Project.Scripts.Services.SafeArea;
using UnityEngine;

namespace Project.Scripts.Services.Layout
{
    public class GameplayScreenLayoutService : IGameplayScreenLayoutService
    {
        private readonly GameplayScreenLayoutConfig _config;
        private readonly ISafeAreaService _safeAreaService;


        public GameplayScreenLayoutService(GameplayScreenLayoutConfig config, ISafeAreaService safeAreaService)
        {
            _config = config;
            _safeAreaService = safeAreaService;
        }


        public GameplayScreenLayout Calculate()
        {
            var safeArea = _safeAreaService.Current.CurrentValue;
            var screenRect = new ScreenLayoutRect(0f, 0f, safeArea.ScreenSize.x, safeArea.ScreenSize.y);
            var safeAreaRect = new ScreenLayoutRect(safeArea.Raw.x, safeArea.Raw.y, safeArea.Raw.width,
                safeArea.Raw.height);

            return GameplayScreenLayoutCalculator.Calculate(
                screenRect,
                safeAreaRect,
                _config.UseSafeArea,
                _config.WorldExtendsIntoUnsafeBottomArea,
                _config.SafeAreaPadding,
                _config.GameplayAspect,
                _config.ReferenceResolutionWidth,
                _config.ReferenceResolutionHeight,
                _config.TopBarHeight,
                _config.TopBarSidePadding,
                _config.TopBarBottomPadding,
                _config.WorldBottomPadding,
                _config.WorldSidePadding);
        }


        public Rect ToUnityRect(ScreenLayoutRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }


        public Rect ToWorldRect(Camera camera, ScreenLayoutRect rect)
        {
            if (!camera)
                return default;

            var min = camera.ScreenToWorldPoint(new Vector3(rect.XMin, rect.YMin, -camera.transform.position.z));
            var max = camera.ScreenToWorldPoint(new Vector3(rect.XMax, rect.YMax, -camera.transform.position.z));
            
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}