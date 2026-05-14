using Project.Scripts.Configs.Battle.Units;
using UnityEngine;

namespace Project.Scripts.Configs.Battle
{
    [CreateAssetMenu(fileName = "PlayerBattleConfig", menuName = "Configs/Battle/Player Battle Config")]
    public class PlayerBattleConfig : ScriptableObject
    {
        [Tooltip("Базовая дека игрока, используемая до появления выбора деки в лобби")]
        [SerializeField] private UnitDeckConfig _defaultUnitDeck;


        public UnitDeckConfig DefaultUnitDeck => _defaultUnitDeck;
    }
}