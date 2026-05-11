using System;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.BattleSetup
{
    public readonly struct BattleSetup
    {
        public const int HeroSlotCount = 4;

        
        public BattleUnitSetup PlayerAvatar { get; }
        public BattleUnitSetup EnemyAvatar { get; }

        
        private readonly BattleUnitSetup[] _playerHeroes;
        private readonly BattleUnitSetup[] _enemyHeroes;


        public BattleSetup(BattleUnitSetup playerAvatar, BattleUnitSetup enemyAvatar,
            BattleUnitSetup[] playerHeroes, BattleUnitSetup[] enemyHeroes)
        {
            PlayerAvatar = playerAvatar;
            EnemyAvatar = enemyAvatar;
            _playerHeroes = CopyHeroes(playerHeroes);
            _enemyHeroes = CopyHeroes(enemyHeroes);
        }

        public bool TryGetUnit(UnitDescriptor unit, out BattleUnitSetup setup)
        {
            if (unit.Kind == UnitKind.Avatar)
            {
                setup = unit.Side == BattleSide.Player ? PlayerAvatar : EnemyAvatar;
                
                return true;
            }

            var heroes = unit.Side == BattleSide.Player ? _playerHeroes : _enemyHeroes;
            if (unit.SlotIndex is < 0 or >= HeroSlotCount || unit.SlotIndex >= heroes.Length)
            {
                setup = default;
                
                return false;
            }

            setup = heroes[unit.SlotIndex];
            
            return true;
        }

        public BattleUnitSetup GetHero(BattleSide side, int slotIndex)
        {
            var heroes = side == BattleSide.Player ? _playerHeroes : _enemyHeroes;
            
            return slotIndex >= 0 && slotIndex < heroes.Length ? heroes[slotIndex] : default;
        }

        private static BattleUnitSetup[] CopyHeroes(BattleUnitSetup[] heroes)
        {
            var result = new BattleUnitSetup[HeroSlotCount];
            if (null == heroes)
                return result;

            Array.Copy(heroes, result, Math.Min(HeroSlotCount, heroes.Length));
            
            return result;
        }
    }
}