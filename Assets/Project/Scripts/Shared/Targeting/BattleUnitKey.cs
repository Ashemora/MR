using System;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Targeting
{
    public readonly struct BattleUnitKey : IEquatable<BattleUnitKey>
    {
        public BattleSide Side { get; }
        public UnitKind Kind { get; }
        public int SlotIndex { get; }


        public BattleUnitKey(BattleSide side, UnitKind kind, int slotIndex)
        {
            Side = side;
            Kind = kind;
            SlotIndex = kind == UnitKind.Avatar ? -1 : slotIndex;
        }

        public static BattleUnitKey FromDescriptor(UnitDescriptor descriptor)
        {
            return new BattleUnitKey(descriptor.Side, descriptor.Kind, descriptor.SlotIndex);
        }

        public bool Equals(BattleUnitKey other)
        {
            return Side == other.Side && Kind == other.Kind && SlotIndex == other.SlotIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is BattleUnitKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Side;
                hashCode = (hashCode * 397) ^ (int)Kind;
                hashCode = (hashCode * 397) ^ SlotIndex;
                
                return hashCode;
            }
        }

        public static bool operator ==(BattleUnitKey left, BattleUnitKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BattleUnitKey left, BattleUnitKey right)
        {
            return false == left.Equals(right);
        }
    }
}