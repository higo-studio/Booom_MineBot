using UnityEngine;
using UnityEngine.SceneManagement;

namespace Minebot.Bootstrap
{
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

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMinebotServiceConsumer consumer)
                {
                    consumer.InjectServices(Services, Config);
                }
            }
        }
    }
}
