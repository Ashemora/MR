namespace Project.Scripts.Shared.Rules
{
    public readonly struct BattleActionGateResult
    {
        public bool IsAllowed => Reason == BattleActionBlockReason.None;
        public BattleActionBlockReason Reason { get; }


        public BattleActionGateResult(BattleActionBlockReason reason)
        {
            Reason = reason;
        }
    }
}
