using Project.Scripts.Shared.Units;

namespace Project.Scripts.Shared.Buffs
{
    public readonly struct ShieldAbsorptionResult
    {
        public UnitDescriptor Target { get; }
        public int IncomingDamage { get; }
        public int AbsorbedDamage { get; }
        public int RemainingDamage { get; }
        public ShieldSnapshot Shield { get; }
        public bool ShieldChanged => AbsorbedDamage > 0;
        public bool WasFullyAbsorbed => IncomingDamage > 0 && RemainingDamage == 0;


        public ShieldAbsorptionResult(UnitDescriptor target, int incomingDamage, int absorbedDamage,
            int remainingDamage, ShieldSnapshot shield)
        {
            Target = target;
            IncomingDamage = incomingDamage < 0 ? 0 : incomingDamage;
            AbsorbedDamage = absorbedDamage < 0 ? 0 : absorbedDamage;
            RemainingDamage = remainingDamage < 0 ? 0 : remainingDamage;
            Shield = shield;
        }
    }
}
