using Project.Scripts.Configs.Gameplay;
using Project.Scripts.Gameplay.Layout;
using UnityEngine;

namespace Project.Scripts.Services.Layout
{
    public class GameplayScreenLayoutService : IGameplayScreenLayoutService
    {
        private readonly GameplayScreenLayoutConfig _config;


        public GameplayScreenLayoutService(GameplayScreenLayoutConfig config)
        {
            _config = config;
        }


        public GameplayScreenLayout Calculate()
        {
            var screenRect = new ScreenLayoutRect(0f, 0f, Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            var safeAreaRect = new ScreenLayoutRect(safeArea.x, safeArea.y, safeArea.width, safeArea.height);

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