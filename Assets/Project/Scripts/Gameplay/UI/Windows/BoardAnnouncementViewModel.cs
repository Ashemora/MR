using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Services.Announcements;
using Project.Scripts.Services.UISystem;
using UnityEngine;

namespace Project.Scripts.Gameplay.UI.Windows
{
    public class BoardAnnouncementViewModel : BaseViewModel
    {
        public AnnouncementStyle Style { get; }
        public string Text { get; }
        public Color TextColor { get; }
        public float DisplayDuration { get; }
        public float FadeOutDuration { get; }
        public float FlyDistance { get; }
        public float BaseScale { get; }
        public float ScaleMultiplier { get; }
        public Ease FadeOutEase { get; }
        public float WorldY { get; }
        public float CurrentWorldY => _worldYGetter?.Invoke() ?? WorldY;


        private readonly UniTaskCompletionSource _animationDone = new();
        private readonly Func<float> _worldYGetter;


        public BoardAnnouncementViewModel(
            AnnouncementStyle style,
            string text,
            Color textColor,
            float displayDuration,
            float fadeOutDuration,
            float flyDistance,
            float baseScale,
            float scaleMultiplier,
            Ease fadeOutEase,
            float worldY,
            Func<float> worldYGetter = null)
        {
            Style = style;
            Text = text;
            TextColor = textColor;
            DisplayDuration = displayDuration;
            FadeOutDuration = fadeOutDuration;
            FlyDistance = flyDistance;
            BaseScale = baseScale;
            ScaleMultiplier = scaleMultiplier;
            FadeOutEase = fadeOutEase;
            WorldY = worldY;
            _worldYGetter = worldYGetter;
        }


        public UniTask WaitAsync()
        {
            return _animationDone.Task;
        }

        public void NotifyAnimationDone()
        {
            _animationDone.TrySetResult();
        }
    }
}