using System.Collections.Generic;
using Project.Scripts.Shared.Passives;

namespace Project.Scripts.Services.Combat.Passives
{
    public interface IHeroPassiveService
    {
        IReadOnlyList<UnitPassiveRuntimeState> States { get; }
    }
}