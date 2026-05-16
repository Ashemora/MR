using R3;

namespace Project.Scripts.Services.SafeArea
{
    public interface ISafeAreaService
    {
        ReadOnlyReactiveProperty<SafeAreaInfo> Current { get; }
    }
}
