using UnityEngine;
using UnityEngine.SceneManagement;

namespace Minebot.Bootstrap
{
    [MinebotRuntimeTag(MinebotRuntimeTag.Provider)]
    public sealed class MinebotRuntimeContext : MonoBehaviour
    {
        public MinebotContainer Container { get; private set; }
        public RuntimeServiceRegistry Services { get; private set; }
        public BootstrapConfig Config { get; private set; }
        public bool IsInitialized => Container != null;

        public RuntimeServiceRegistry Initialize(BootstrapConfig config)
        {
            if (Container != null)
            {
                return Services;
            }

            Config = config;
            Container = RuntimeServiceFactory.CreateContainer(config);
            Services = Container.Resolve<RuntimeServiceRegistry>();
            MinebotServices.SetCurrentContainer(Container);
            return Services;
        }

        public MinebotContainer GetContainer()
        {
            return Container;
        }

        public void InjectIntoScene(Scene scene)
        {
            if (!IsInitialized || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                InjectIntoHierarchy(root);
            }
        }

        public void InjectIntoHierarchy(GameObject root)
        {
            if (!IsInitialized || root == null)
            {
                return;
            }

            MinebotRuntimeDiscovery.InjectIntoHierarchy(root, Container);
        }
    }
}
