using UnityEngine;

namespace Minebot.Bootstrap
{
    public static class MinebotServices
    {
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

            Current = RuntimeServiceFactory.Create(config);
            return Current;
        }

        public static void SetCurrent(RuntimeServiceRegistry services)
        {
            Current = services;
        }

        public static void ResetForTests()
        {
            Current = null;
        }
    }
}
