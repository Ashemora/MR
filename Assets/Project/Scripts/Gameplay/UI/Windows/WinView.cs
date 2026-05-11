using Cysharp.Threading.Tasks;
using Project.Scripts.Services.UISystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Gameplay.UI.Windows
{
    public class WinView : BaseView<WinViewModel>
    {
        [Tooltip("Текст с количеством использованных ходов для победы")]
        [SerializeField] private TMP_Text _movesText;

        [Tooltip("Текст с ID текущего уровня")]
        [SerializeField] private TMP_Text _levelIdText;

        [Tooltip("Текст с именем побеждённого противника")]
        [SerializeField] private TMP_Text _opponentNameText;

        [Tooltip("Кнопка перехода на следующий уровень")]
        [SerializeField] private Button _nextLevelButton;


        protected override bool EnablePumpAnimation => true;


        protected override UniTask OnBindViewModel()
        {
            _movesText.text = ViewModel.MovesUsed.ToString();
            _levelIdText.text = ViewModel.LevelId.ToString();
            _opponentNameText.text = ViewModel.OpponentName;
            _nextLevelButton.onClick.AddListener(ViewModel.NextLevel);
            return UniTask.CompletedTask;
        }

        protected override void OnClose()
        {
            _nextLevelButton.onClick.RemoveAllListeners();
        }
    }
}