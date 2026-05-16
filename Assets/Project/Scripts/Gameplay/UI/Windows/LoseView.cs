using Cysharp.Threading.Tasks;
using Project.Scripts.Services.UISystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Gameplay.UI.Windows
{
    public class LoseView : BaseView<LoseViewModel>
    {
        [Tooltip("Текст с количеством использованных ходов")]
        [SerializeField] private TMP_Text _movesText;

        [Tooltip("Текст с ID текущего уровня")]
        [SerializeField] private TMP_Text _levelIdText;

        [Tooltip("Текст с именем противника")]
        [SerializeField] private TMP_Text _opponentNameText;

        [Tooltip("Кнопка повторного прохождения текущего уровня")]
        [SerializeField] private Button _retryButton;


        public override SafeAreaMode SafeAreaMode => SafeAreaMode.ForceIgnore;

        protected override bool EnablePumpAnimation => true;


        protected override UniTask OnBindViewModel()
        {
            _movesText.text = ViewModel.MovesUsed.ToString();
            _levelIdText.text = ViewModel.LevelId.ToString();
            _opponentNameText.text = ViewModel.OpponentName;
            _retryButton.onClick.AddListener(ViewModel.Retry);
            return UniTask.CompletedTask;
        }

        protected override void OnClose()
        {
            _retryButton.onClick.RemoveAllListeners();
        }
    }
}