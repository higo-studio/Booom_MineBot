using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Minebot.Bootstrap
{
    public static class MinebotRuntimeDiscovery
    {
        public const string InjectServicesMethodName = "InjectServices";
        public const string GetContainerMethodName = "GetContainer";

        private static readonly Dictionary<Type, TaggedMethodCache> CacheByType = new Dictionary<Type, TaggedMethodCache>();
        private static readonly object[] NoArguments = Array.Empty<object>();

        public static void InjectIntoHierarchy(GameObject root, MinebotContainer container, MonoBehaviour excludedConsumer = null)
        {
            if (root == null || container == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || ReferenceEquals(behaviour, excludedConsumer))
                {
                    continue;
                }

                TryInjectInto(behaviour, container);
            }
        }

        public static bool TryInjectInto(MonoBehaviour behaviour)
        {
            if (!TryResolveContainer(out MinebotContainer container))
            {
                return false;
            }

            return TryInjectInto(behaviour, container);
        }

        public static bool TryInjectInto(MonoBehaviour behaviour, MinebotContainer container)
        {
            if (behaviour == null || container == null)
            {
                return false;
            }

            TaggedMethodCache cache = GetOrCreateCache(behaviour.GetType());
            if (!cache.IsConsumer || cache.InjectMethods.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < cache.InjectMethods.Length; i++)
            {
                MethodInfo method = cache.InjectMethods[i];
                if (!container.TryBuildArguments(method.GetParameters(), out object[] arguments))
                {
                    continue;
                }

                method.Invoke(behaviour, arguments);
                return true;
            }

            return false;
        }

        public static bool TryResolveContainer(out MinebotContainer container)
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
                if (!cache.IsProvider || cache.GetContainerMethod == null)
                {
                    continue;
                }

                if (cache.GetContainerMethod.Invoke(behaviour, NoArguments) is not MinebotContainer candidateContainer || candidateContainer == null)
                {
                    continue;
                }

                container = candidateContainer;
                return true;
            }

            container = null;
            return false;
        }

        public static bool TryResolveRuntimeServices(out RuntimeServiceRegistry services, out BootstrapConfig config)
        {
            if (!TryResolveContainer(out MinebotContainer container)
                || !container.TryResolve(out services)
                || services == null)
            {
                services = null;
                config = null;
                return false;
            }

            container.TryResolve(out config);
            return true;
        }

        public static bool TryResolveBootstrapConfig(out BootstrapConfig config)
        {
            if (!TryResolveContainer(out MinebotContainer container)
                || !container.TryResolve(out config)
                || config == null)
            {
                config = null;
                return false;
            }

            return true;
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
                InjectMethods = ResolveConsumerInjectMethods(behaviourType),
                GetContainerMethod = ResolveProviderMethod(behaviourType, GetContainerMethodName, typeof(MinebotContainer))
            };
            CacheByType.Add(behaviourType, cache);
            return cache;
        }

        private static MethodInfo[] ResolveConsumerInjectMethods(Type behaviourType)
        {
            MethodInfo[] methods = behaviourType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var matches = new List<MethodInfo>();
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!string.Equals(method.Name, InjectServicesMethodName, StringComparison.Ordinal)
                    || method.ReturnType != typeof(void))
                {
                    continue;
                }

                matches.Add(method);
            }

            matches.Sort((left, right) => right.GetParameters().Length.CompareTo(left.GetParameters().Length));
            return matches.ToArray();
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
            public MethodInfo[] InjectMethods = Array.Empty<MethodInfo>();
            public MethodInfo GetContainerMethod;
        }
    }
}
