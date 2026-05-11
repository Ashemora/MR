using Project.Scripts.Gameplay.Layout;
using UnityEngine;

namespace Project.Scripts.Services.Layout
{
    public interface IGameplayScreenLayoutService
    {
        GameplayScreenLayout Calculate();
        Rect ToUnityRect(ScreenLayoutRect rect);
        Rect ToWorldRect(Camera camera, ScreenLayoutRect rect);
    }
}