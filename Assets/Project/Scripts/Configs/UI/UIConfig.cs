using UnityEngine;

namespace Project.Scripts.Configs.UI
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "Configs/UI Config")]
    public class UIConfig : ScriptableObject
    {
        [Tooltip("Префаб компонента WinView - отображается, когда игрок побеждает врага")]
        [SerializeField] private GameObject _winViewPrefab;

        [Tooltip("Префаб компонента LoseView - отображается, когда у игрока заканчиваются ходы")]
        [SerializeField] private GameObject _loseViewPrefab;

        [Tooltip("Префаб компонента MoveBarView - прикреплён к нижней части экрана")]
        [SerializeField] private GameObject _moveBarViewPrefab;

        [Tooltip("Префаб компонента TopBarView - имя врага и второстепенный ярлык, остаётся в Canvas")]
        [SerializeField] private GameObject _topBarViewPrefab;

        [Tooltip("Префаб экрана загрузки перед переходом из лобби в бой")]
        [SerializeField] private GameObject _gameplayLoadingViewPrefab;

        [Tooltip("Префаб окна опций - открывается из лобби, содержит настройки звука")]
        [SerializeField] private GameObject _optionsViewPrefab;


        public GameObject WinViewPrefab => _winViewPrefab;
        public GameObject LoseViewPrefab => _loseViewPrefab;
        public GameObject MoveBarViewPrefab => _moveBarViewPrefab;
        public GameObject TopBarViewPrefab => _topBarViewPrefab;
        public GameObject GameplayLoadingViewPrefab => _gameplayLoadingViewPrefab;
        public GameObject OptionsViewPrefab => _optionsViewPrefab;
    }
}