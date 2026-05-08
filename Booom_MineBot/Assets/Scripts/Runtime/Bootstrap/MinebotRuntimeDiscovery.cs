using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Minebot.Bootstrap
{
    public static class MinebotRuntimeDiscovery
    {
        public const string InjectServicesMethodName = "InjectServices";
        public const string GetServicesMethodName = "GetServices";
        public const string GetBootstrapConfigMethodName = "GetBootstrapConfig";

        private static readonly Dictionary<Type, TaggedMethodCache> CacheByType = new Dictionary<Type, TaggedMethodCache>();
        private static readonly object[] ProviderConfigArguments = Array.Empty<object>();

        public static void InjectIntoHierarchy(GameObject root, RuntimeServiceRegistry services, BootstrapConfig config, MonoBehaviour excludedConsumer = null)
        {
            if (root == null || services == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            object[] arguments = { services, config };
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || ReferenceEquals(behaviour, excludedConsumer))
                {
                    continue;
                }

                if (!TryGetConsumerInjectionMethod(behaviour.GetType(), out MethodInfo method))
                {
                    continue;
                }

                method.Invoke(behaviour, arguments);
            }
        }

        public static bool TryResolveRuntimeServices(out RuntimeServiceRegistry services, out BootstrapConfig config)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                TaggedMethodCache cache = GetOrCreateCache(behaviour.GetType());
                if (!cache.IsProvider || cache.GetServicesMethod == null)
                {
                    continue;
                }

                if (cache.GetServicesMethod.Invoke(behaviour, ProviderConfigArguments) is not RuntimeServiceRegistry candidateServices || candidateServices == null)
                {
                    continue;
                }

                services = candidateServices;
                config = cache.GetBootstrapConfigMethod != null
                    ? cache.GetBootstrapConfigMethod.Invoke(behaviour, ProviderConfigArguments) as BootstrapConfig
                    : null;
                return true;
            }

            services = null;
            config = null;
            return false;
        }

        public static bool TryResolveBootstrapConfig(out BootstrapConfig config)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                TaggedMethodCache cache = GetOrCreateCache(behaviour.GetType());
                if (!cache.IsProvider || cache.GetBootstrapConfigMethod == null)
                {
                    continue;
                }

                if (cache.GetBootstrapConfigMethod.Invoke(behaviour, ProviderConfigArguments) is BootstrapConfig candidateConfig && candidateConfig != null)
                {
                    config = candidateConfig;
                    return true;
                }
            }

            config = null;
            return false;
        }

        private static bool TryGetConsumerInjectionMethod(Type behaviourType, out MethodInfo method)
        {
            TaggedMethodCache cache = GetOrCreateCache(behaviourType);
            method = cache.InjectMethod;
            return cache.IsConsumer && method != null;
        }

        private static TaggedMethodCache GetOrCreateCache(Type behaviourType)
        {
            if (CacheByType.TryGetValue(behaviourType, out TaggedMethodCache cache))
            {
                return cache;
            }

            MinebotRuntimeTagAttribute tag = behaviourType.GetCustomAttribute<MinebotRuntimeTagAttribute>(false);
            cache = new TaggedMethodCache
            {
                IsProvider = tag != null && tag.Tag == MinebotRuntimeTag.Provider,
                IsConsumer = tag != null && tag.Tag == MinebotRuntimeTag.Consumer,
                InjectMethod = ResolveConsumerInjectMethod(behaviourType),
                GetServicesMethod = ResolveProviderMethod(behaviourType, GetServicesMethodName, typeof(RuntimeServiceRegistry)),
                GetBootstrapConfigMethod = ResolveProviderMethod(behaviourType, GetBootstrapConfigMethodName, typeof(BootstrapConfig))
            };
            CacheByType.Add(behaviourType, cache);
            return cache;
        }

        private static MethodInfo ResolveConsumerInjectMethod(Type behaviourType)
        {
            MethodInfo method = behaviourType.GetMethod(
                InjectServicesMethodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(RuntimeServiceRegistry), typeof(BootstrapConfig) },
                null);
            return method != null && method.ReturnType == typeof(void) ? method : null;
        }

        private static MethodInfo ResolveProviderMethod(Type behaviourType, string methodName, Type returnType)
        {
            MethodInfo method = behaviourType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);
            return method != null && method.ReturnType == returnType ? method : null;
        }

        private sealed class TaggedMethodCache
        {
            public bool IsProvider;
            public bool IsConsumer;
            public MethodInfo InjectMethod;
            public MethodInfo GetServicesMethod;
            public MethodInfo GetBootstrapConfigMethod;
        }
    }
}
