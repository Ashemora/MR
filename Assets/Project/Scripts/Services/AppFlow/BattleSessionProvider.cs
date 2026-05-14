using Project.Scripts.Shared.Match;

namespace Project.Scripts.Services.AppFlow
{
    public class BattleSessionProvider : IBattleSessionProvider
    {
        public BattleSession Current { get; private set; }


        public void SetCurrent(BattleSession session)
        {
            Current = session;
        }

        public void Clear()
        {
            Current = null;
        }
    }
}