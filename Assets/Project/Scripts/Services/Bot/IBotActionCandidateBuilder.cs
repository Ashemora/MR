using System.Collections.Generic;
using Project.Scripts.Shared.Bot;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Services.Bot
{
    public interface IBotActionCandidateBuilder
    {
        void AddCandidatesForSource(UnitDescriptor source, List<BotActionCandidate> result);
    }
}