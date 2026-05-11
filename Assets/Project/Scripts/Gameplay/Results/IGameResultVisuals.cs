using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.UI;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Gameplay.Results
{
    public interface IGameResultVisuals
    {
        UniTask PlayAvatarPulse(BattleSide side, AvatarPulseStepConfig config);
    }
}