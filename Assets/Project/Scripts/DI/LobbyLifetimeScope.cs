using Project.Scripts.Lobby;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.DI
{
    public class LobbyLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<LobbyView>();
            builder.RegisterComponentInHierarchy<LobbyController>();
        }
    }
}