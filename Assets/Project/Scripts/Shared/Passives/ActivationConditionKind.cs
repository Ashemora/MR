namespace Project.Scripts.Shared.Passives
{
    public enum ActivationConditionKind
    {
        None,
        MatchEnergyCollected,
        MatchesCollected,
        LineRuneUsed,
        BombUsed,
        StormUsed,
        SlotKindMatchesCollected,
        UnitActivationsInTimeWindow,
        SlotKindMatchesInTimeWindow,
        EnemyHeroDefeatsInTimeWindow
    }
}