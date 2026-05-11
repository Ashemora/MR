namespace Project.Scripts.Shared.ActivationConditions
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