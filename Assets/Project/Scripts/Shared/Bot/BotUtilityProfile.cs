namespace Project.Scripts.Shared.Bot
{
    public readonly struct BotUtilityProfile
    {
        public readonly float DamagePreference;
        public readonly float HealPreference;
        public readonly float ResurrectPreference;
        public readonly float SupportPreference;
        public readonly float FinishEnemyWeight;
        public readonly float BreakDefenseWeight;
        public readonly float AttackExposedAvatarWeight;
        public readonly float ProtectOwnAvatarWeight;
        public readonly float HealLowHpAllyWeight;
        public readonly float ResurrectAllyWeight;
        public readonly float AvoidOverhealWeight;
        public readonly float AvoidOverkillWeight;
        public readonly float HealThroughShieldPenaltyWeight;


        public BotUtilityProfile(float damagePreference, float healPreference, float resurrectPreference,
            float supportPreference, float finishEnemyWeight, float breakDefenseWeight,
            float attackExposedAvatarWeight, float protectOwnAvatarWeight, float healLowHpAllyWeight,
            float resurrectAllyWeight, float avoidOverhealWeight, float avoidOverkillWeight,
            float healThroughShieldPenaltyWeight)
        {
            DamagePreference = ClampNonNegative(damagePreference);
            HealPreference = ClampNonNegative(healPreference);
            ResurrectPreference = ClampNonNegative(resurrectPreference);
            SupportPreference = ClampNonNegative(supportPreference);
            FinishEnemyWeight = ClampNonNegative(finishEnemyWeight);
            BreakDefenseWeight = ClampNonNegative(breakDefenseWeight);
            AttackExposedAvatarWeight = ClampNonNegative(attackExposedAvatarWeight);
            ProtectOwnAvatarWeight = ClampNonNegative(protectOwnAvatarWeight);
            HealLowHpAllyWeight = ClampNonNegative(healLowHpAllyWeight);
            ResurrectAllyWeight = ClampNonNegative(resurrectAllyWeight);
            AvoidOverhealWeight = ClampNonNegative(avoidOverhealWeight);
            AvoidOverkillWeight = ClampNonNegative(avoidOverkillWeight);
            HealThroughShieldPenaltyWeight = ClampNonNegative(healThroughShieldPenaltyWeight);
        }
        

        private static float ClampNonNegative(float value)
        {
            return value < 0f ? 0f : value;
        }
    }
}