using UnityEngine;

namespace Project.Scripts.Configs.Battle.Units
{
    [CreateAssetMenu(fileName = "UnitDeckConfig", menuName = "Configs/Battle/Unit Deck Config")]
    public class UnitDeckConfig : ScriptableObject
    {
        [Tooltip("Отображаемое имя деки для будущего UI выбора")]
        [SerializeField] private string _displayName;

        [Tooltip("Конфиг аватара этой деки")]
        [SerializeField] private AvatarConfig _avatarConfig;

        [Tooltip("Четыре конфига героев этой деки (пустой слот = null)")]
        [SerializeField] private HeroConfig[] _heroes = new HeroConfig[4];


        public string DisplayName => _displayName;
        public AvatarConfig AvatarConfig => _avatarConfig;
        public HeroConfig[] Heroes => _heroes;
    }
}
