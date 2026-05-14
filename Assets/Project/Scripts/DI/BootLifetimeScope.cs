using Project.Scripts.Bootstrap;
using VContainer;
using VContainer.Unity;

namespace Project.Scripts.DI
{
    public class BootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<BootstrapController>();
        }
    }
}