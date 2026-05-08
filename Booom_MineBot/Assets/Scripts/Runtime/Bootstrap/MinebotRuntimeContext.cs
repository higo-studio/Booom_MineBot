using UnityEngine;
using UnityEngine.SceneManagement;

namespace Minebot.Bootstrap
{
    [MinebotRuntimeTag(MinebotRuntimeTag.Provider)]
    public sealed class MinebotRuntimeContext : MonoBehaviour
    {
        public RuntimeServiceRegistry Services { get; private set; }
        public BootstrapConfig Config { get; private set; }
        public bool IsInitialized => Services != null;

        public RuntimeServiceRegistry Initialize(BootstrapConfig config)
        {
            if (Services != null)
            {
                return Services;
            }

            Config = config;
            Services = RuntimeServiceFactory.Create(config);
            MinebotServices.SetCurrent(Services);
            return Services;
        }

        public RuntimeServiceRegistry GetServices()
        {
            return Services;
        }

        public BootstrapConfig GetBootstrapConfig()
        {
            return Config;
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

            MinebotRuntimeDiscovery.InjectIntoHierarchy(root, Services, Config);
        }
    }
}
