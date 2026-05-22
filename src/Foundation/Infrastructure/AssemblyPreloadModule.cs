using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace Foundation.Infrastructure
{
    [InitializableModule]
    public class AssemblyPreloadModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context) { }
        public void Uninitialize(InitializationEngine context) { }
    }
}
