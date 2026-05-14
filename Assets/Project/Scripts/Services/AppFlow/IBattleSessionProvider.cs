using Project.Scripts.Shared.Match;

namespace Project.Scripts.Services.AppFlow
{
    public interface IBattleSessionProvider
    {
        BattleSession Current { get; }
        void SetCurrent(BattleSession session);
        void Clear();
    }
}