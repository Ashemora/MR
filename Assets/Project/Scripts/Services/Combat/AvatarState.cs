using Project.Scripts.Shared.Heroes;

namespace Project.Scripts.Services.Combat
{
    public readonly struct AvatarState
    {
        public BattleSide Side { get; }
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public bool IsAssigned { get; }
        public bool IsAlive => MaxHP <= 0 || CurrentHP > 0;
        public bool IsHpFull => MaxHP > 0 && CurrentHP >= MaxHP;


        public AvatarState(BattleSide side, int currentHP, int maxHP, bool isAssigned)
        {
            Side = side;
            CurrentHP = currentHP < 0 ? 0 : currentHP;
            MaxHP = maxHP < 0 ? 0 : maxHP;
            IsAssigned = isAssigned;
        }
    }
}
