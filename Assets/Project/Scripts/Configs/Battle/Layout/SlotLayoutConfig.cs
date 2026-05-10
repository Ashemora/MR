using Project.Scripts.Shared.Tiles;
using UnityEngine;

namespace Project.Scripts.Configs.Battle.Layout
{
    [CreateAssetMenu(fileName = "SlotLayoutConfig", menuName = "Configs/Slot Layout Config")]
    public class SlotLayoutConfig : ScriptableObject
    {
        [Tooltip("Типы тайлов для 4 слотов героев (слева направо, не включая слот аватара)")]
        [SerializeField] private TileKind[] _heroSlotKinds = new TileKind[4];

        [Tooltip("Тип тайлов слота аватара - тайлы этого типа вносят наибольший вклад в заряд аватара")]
        [SerializeField] private TileKind _avatarSlotKind = TileKind.Void;

        [Header("Avatar Group Defense")]
        [Tooltip("Индексы слотов героев, составляющих Группу 1 (синяя + красная ячейки)")]
        [SerializeField] private int[] _group1SlotIndices = { 0, 1 };

        [Tooltip("Индексы слотов героев, составляющих Группу 2 (зелёная + жёлтая ячейки)")]
        [SerializeField] private int[] _group2SlotIndices = { 2, 3 };


        public TileKind[] HeroSlotKinds => _heroSlotKinds;
        public TileKind AvatarSlotKind => _avatarSlotKind;
        public int[] Group1SlotIndices => _group1SlotIndices;
        public int[] Group2SlotIndices => _group2SlotIndices;
    }
}