using Cysharp.Threading.Tasks;
using Project.Scripts.Services.SceneLoading;
using Project.Scripts.Services.UISystem;
using TMPro;
using UnityEngine;

namespace Project.Scripts.Gameplay.UI.Loading
{
    public class GameplayLoadingView : BaseView<GameplayLoadingViewModel>, ILoadingPresenter
    {
        [Tooltip("RectTransform stretched horizontally to display gameplay scene loading progress")]
        [SerializeField] private RectTransform _progressFill;

        [Tooltip("Optional progress label")]
        [SerializeField] private TMP_Text _progressText;


        protected override UniTask OnBindViewModel()
        {
            SetProgress(0f);
            
            return UniTask.CompletedTask;
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (_progressFill)
                _progressFill.anchorMax = new Vector2(progress, _progressFill.anchorMax.y);

            if (_progressText)
                _progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }
    }
}