using UnityEngine;

namespace Project.Scripts.Configs.Battle.Units
{
    [CreateAssetMenu(fileName = "UnitDeathConfig", menuName = "Configs/Battle/Unit Death Config")]
    public class UnitDeathConfig : ScriptableObject
    {
        [Tooltip("Настройки визуального состояния героя при гибели.")]
        [SerializeField] private HeroDeathVisuals _heroDeathVisuals;

        [Space(10)]
        [Tooltip("Настройки визуального состояния аватара при гибели.")]
        [SerializeField] private AvatarDeathVisuals _avatarDeathVisuals;


        public HeroDeathVisuals HeroDeathVisuals => _heroDeathVisuals;
        public AvatarDeathVisuals AvatarDeathVisuals => _avatarDeathVisuals;


        private void Reset()
        {
            _heroDeathVisuals = new HeroDeathVisuals
            {
                ApplyDeathFill = true,
                DeathColor = new Color(0.3f, 0.3f, 0.3f, 1f)
            };
            _avatarDeathVisuals = new AvatarDeathVisuals
            {
                ApplyDeathFill = true,
                DeathColor = new Color(0.298f, 0.298f, 0.298f, 1f)
            };
        }
    }
}