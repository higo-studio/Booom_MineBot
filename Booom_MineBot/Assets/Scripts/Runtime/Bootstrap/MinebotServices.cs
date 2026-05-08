using UnityEngine;

namespace Minebot.Bootstrap
{
    public static class MinebotServices
    {
        public static MinebotContainer CurrentContainer { get; private set; }
        public static RuntimeServiceRegistry Current { get; private set; }
        public static bool IsInitialized => Current != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnPlayStart()
        {
            Current = null;
        }

        public static RuntimeServiceRegistry Initialize(BootstrapConfig config)
        {
            if (Current != null)
            {
                return Current;
            }

            SetCurrentContainer(RuntimeServiceFactory.CreateContainer(config));
            return Current;
        }

        public static void SetCurrent(RuntimeServiceRegistry services)
        {
            CurrentContainer = null;
            Current = services;
        }

        public static void SetCurrentContainer(MinebotContainer container)
        {
            CurrentContainer = container;
            Current = container != null ? container.Resolve<RuntimeServiceRegistry>() : null;
        }

        public static void ResetForTests()
        {
            CurrentContainer = null;
            Current = null;
        }
    }
}
