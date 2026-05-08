using System;
using System.Collections.Generic;
using System.Reflection;

namespace Minebot.Bootstrap
{
    public sealed class MinebotContainer
    {
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        private readonly HashSet<Type> resolutionChain = new HashSet<Type>();

        public void RegisterInstance<T>(T instance)
        {
            registrations[typeof(T)] = Registration.ForInstance(typeof(T), instance);
        }

        public void RegisterSingleton<T>(Func<MinebotContainer, T> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            registrations[typeof(T)] = Registration.ForFactory(typeof(T), container => factory(container));
        }

        public void RegisterSingleton<T>() where T : class
        {
            registrations[typeof(T)] = Registration.ForFactory(typeof(T), container => container.InstantiateRegisteredType(typeof(T)));
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            if (TryResolve(type, out object instance))
            {
                return instance;
            }

            throw new InvalidOperationException($"Type '{type?.FullName}' is not registered in the Minebot runtime container.");
        }

        public bool TryResolve<T>(out T instance)
        {
            if (TryResolve(typeof(T), out object resolved))
            {
                instance = (T)resolved;
                return true;
            }

            instance = default;
            return false;
        }

        public bool TryResolve(Type type, out object instance)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!registrations.TryGetValue(type, out Registration registration))
            {
                instance = null;
                return false;
            }

            instance = registration.GetOrCreate(this);
            return true;
        }

        public bool TryBuildArguments(ParameterInfo[] parameters, out object[] arguments)
        {
            arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                if (TryResolve(parameter.ParameterType, out object resolved))
                {
                    arguments[i] = resolved;
                    continue;
                }

                if (parameter.HasDefaultValue)
                {
                    arguments[i] = NormalizeDefaultValue(parameter);
                    continue;
                }

                arguments = null;
                return false;
            }

            return true;
        }

        internal object InstantiateRegisteredType(Type implementationType)
        {
            if (implementationType == null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            ConstructorInfo[] constructors = implementationType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            Array.Sort(constructors, CompareConstructorsByParameterCountDescending);
            for (int i = 0; i < constructors.Length; i++)
            {
                ConstructorInfo constructor = constructors[i];
                if (!TryBuildArguments(constructor.GetParameters(), out object[] arguments))
                {
                    continue;
                }

                return constructor.Invoke(arguments);
            }

            throw new InvalidOperationException(
                $"No public constructor on '{implementationType.FullName}' can be satisfied by the Minebot runtime container.");
        }

        private void EnterResolution(Type type)
        {
            if (!resolutionChain.Add(type))
            {
                throw new InvalidOperationException($"Circular runtime service dependency detected while resolving '{type.FullName}'.");
            }
        }

        private void ExitResolution(Type type)
        {
            resolutionChain.Remove(type);
        }

        private static int CompareConstructorsByParameterCountDescending(ConstructorInfo left, ConstructorInfo right)
        {
            return right.GetParameters().Length.CompareTo(left.GetParameters().Length);
        }

        private static object NormalizeDefaultValue(ParameterInfo parameter)
        {
            object value = parameter.DefaultValue;
            if (ReferenceEquals(value, DBNull.Value) || ReferenceEquals(value, Missing.Value))
            {
                return parameter.ParameterType.IsValueType
                    ? Activator.CreateInstance(parameter.ParameterType)
                    : null;
            }

            return value;
        }

        private sealed class Registration
        {
            private readonly Type serviceType;
            private readonly Func<MinebotContainer, object> factory;
            private readonly bool hasPrebuiltInstance;
            private object cachedInstance;
            private bool hasCachedInstance;

            private Registration(Type serviceType, Func<MinebotContainer, object> factory, object cachedInstance, bool hasCachedInstance, bool hasPrebuiltInstance)
            {
                this.serviceType = serviceType;
                this.factory = factory;
                this.cachedInstance = cachedInstance;
                this.hasCachedInstance = hasCachedInstance;
                this.hasPrebuiltInstance = hasPrebuiltInstance;
            }

            public static Registration ForInstance(Type serviceType, object instance)
            {
                return new Registration(serviceType, null, instance, true, true);
            }

            public static Registration ForFactory(Type serviceType, Func<MinebotContainer, object> factory)
            {
                return new Registration(serviceType, factory, null, false, false);
            }

            public object GetOrCreate(MinebotContainer container)
            {
                if (hasCachedInstance)
                {
                    return cachedInstance;
                }

                if (hasPrebuiltInstance)
                {
                    hasCachedInstance = true;
                    return cachedInstance;
                }

                container.EnterResolution(serviceType);
                try
                {
                    cachedInstance = factory(container);
                    hasCachedInstance = true;
                    return cachedInstance;
                }
                finally
                {
                    container.ExitResolution(serviceType);
                }
            }
        }
    }
}
