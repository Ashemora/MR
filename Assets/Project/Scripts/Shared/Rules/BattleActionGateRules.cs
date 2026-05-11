namespace Project.Scripts.Shared.Rules
{
    public static class BattleActionGateRules
    {
        public static BattleActionGateResult Evaluate(BattleActionPhase phase, BattleActionKind actionKind)
        {
            if (phase == BattleActionPhase.PrePhase)
                return new BattleActionGateResult(BattleActionBlockReason.PrePhase);

            if (phase == BattleActionPhase.MatchPhase)
            {
                return actionKind == BattleActionKind.BoardSwap
                    ? new BattleActionGateResult(BattleActionBlockReason.None)
                    : new BattleActionGateResult(BattleActionBlockReason.MatchPhase);
            }

            if (phase == BattleActionPhase.HeroPhase)
            {
                return actionKind == BattleActionKind.BoardSwap
                    ? new BattleActionGateResult(BattleActionBlockReason.HeroPhase)
                    : new BattleActionGateResult(BattleActionBlockReason.None);
            }

            if (phase == BattleActionPhase.Burndown)
                return new BattleActionGateResult(BattleActionBlockReason.Burndown);

            if (phase == BattleActionPhase.Finished)
                return new BattleActionGateResult(BattleActionBlockReason.Finished);

            return new BattleActionGateResult(BattleActionBlockReason.None);
        }
    }
}
