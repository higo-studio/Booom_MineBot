using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using McpBridge;

namespace McpBridge.Editor
{
    [McpPluginToolType]
    internal sealed class McpReflectionTools
    {
        [McpPluginTool("unity.reflection_method_find", Title = "Find Reflection Methods")]
        [Description("Finds whitelisted methods by type or method name. This high-risk tool is disabled by default in Settings.")]
        public ToolCallResult FindMethods(
            [Description("Optional type full name filter.")]
            string typeName = null,
            [Description("Optional method name filter.")]
            string methodName = null,
            [Description("Maximum number of results.")]
            int maxResults = 100)
        {
            maxResults = Math.Max(1, Math.Min(maxResults, 200));
            var items = new List<object>();
            foreach (var method in ReflectionPolicy.EnumerateAllowedMethods())
            {
                if (!string.IsNullOrWhiteSpace(typeName) &&
                    !string.Equals(method.DeclaringType?.FullName, typeName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(methodName) &&
                    !string.Equals(method.Name, methodName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                items.Add(ReflectionMethodSnapshot.FromMethod(method).ToDictionary());
                if (items.Count >= maxResults)
                {
                    break;
                }
            }

            return new ToolCallResult
            {
                Text = $"[Success] Found {items.Count} method(s).",
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = true,
                    ["items"] = items
                }
            };
        }

        [McpPluginTool("unity.reflection_method_call", Title = "Call Reflection Method")]
        [Description("Calls a whitelisted method. This high-risk tool is disabled by default in Settings.")]
        public ToolCallResult CallMethod(
            [Description("Declaring type full name.")]
            string typeName,
            [Description("Method name.")]
            string methodName,
            [Description("Optional target object id for instance methods.")]
            string targetObjectId = null,
            [Description("Optional primitive arguments.")]
            object[] arguments = null,
            [Description("Optional explicit parameter type names to disambiguate overloads.")]
            string[] parameterTypeNames = null)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("call a reflection method", out var blocked))
                {
                    return blocked;
                }

                var candidate = ReflectionPolicy.ResolveMethod(typeName, methodName, arguments, parameterTypeNames, out var failure);
                if (candidate == null)
                {
                    return CreateStatus("not_found", failure ?? "[Failed] Could not resolve reflection method.", false);
                }

                object target = null;
                if (!candidate.IsStatic)
                {
                    target = UnityObjectLocator.TryResolveObject(targetObjectId);
                    if (target == null || !candidate.DeclaringType.IsInstanceOfType(target))
                    {
                        return CreateStatus("not_found", $"[Failed] Could not resolve instance target '{targetObjectId}' for method '{candidate.Name}'.", false);
                    }
                }

                try
                {
                    var converted = ReflectionPolicy.ConvertArguments(candidate, arguments);
                    var result = candidate.Invoke(target, converted);
                    return new ToolCallResult
                    {
                        Text = $"[Success] Reflection call '{candidate.DeclaringType?.FullName}.{candidate.Name}' completed.",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["method"] = ReflectionMethodSnapshot.FromMethod(candidate).ToDictionary(),
                            ["result"] = ReflectionResultSerializer.Serialize(result)
                        }
                    };
                }
                catch (Exception exception)
                {
                    var inner = exception is TargetInvocationException invocation && invocation.InnerException != null
                        ? invocation.InnerException
                        : exception;
                    return new ToolCallResult
                    {
                        Text = $"[Failed] Reflection call threw: {inner.Message}",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = false,
                            ["status"] = "failed",
                            ["exceptionType"] = inner.GetType().FullName,
                            ["message"] = inner.Message
                        }
                    };
                }
            });
        }

        private static ToolCallResult CreateStatus(string status, string text, bool ok)
        {
            return new ToolCallResult
            {
                Text = text,
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = ok,
                    ["status"] = status
                }
            };
        }
    }

    internal static class ReflectionPolicy
    {
        private static readonly string[] s_AllowedAssemblyPrefixes = { "Assembly-CSharp", "McpBridge" };

        public static IEnumerable<MethodInfo> EnumerateAllowedMethods()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name ?? string.Empty;
                if (!s_AllowedAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    continue;
                }

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type == null || type.IsGenericTypeDefinition)
                    {
                        continue;
                    }

                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                    {
                        if (method.IsGenericMethodDefinition || method.ContainsGenericParameters)
                        {
                            continue;
                        }

                        if (!IsMethodAllowed(type, method))
                        {
                            continue;
                        }

                        yield return method;
                    }
                }
            }
        }

        private static bool IsMethodAllowed(Type declaringType, MethodInfo method)
        {
            return method.GetCustomAttribute<McpReflectionAllowedAttribute>() != null ||
                   declaringType.GetCustomAttribute<McpReflectionAllowedAttribute>() != null;
        }

        public static MethodInfo ResolveMethod(string typeName, string methodName, object[] arguments, string[] parameterTypeNames, out string failure)
        {
            failure = null;
            var argCount = arguments?.Length ?? 0;
            var candidates = EnumerateAllowedMethods()
                .Where(method =>
                    string.Equals(method.DeclaringType?.FullName, typeName, StringComparison.Ordinal) &&
                    string.Equals(method.Name, methodName, StringComparison.Ordinal) &&
                    method.GetParameters().Length == argCount)
                .ToList();

            if (parameterTypeNames != null && parameterTypeNames.Length > 0)
            {
                candidates = candidates.Where(method =>
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != parameterTypeNames.Length)
                    {
                        return false;
                    }

                    for (var index = 0; index < parameters.Length; index++)
                    {
                        if (!string.Equals(parameters[index].ParameterType.FullName, parameterTypeNames[index], StringComparison.Ordinal))
                        {
                            return false;
                        }
                    }

                    return true;
                }).ToList();
            }

            foreach (var candidate in candidates)
            {
                if (CanConvertArguments(candidate, arguments))
                {
                    return candidate;
                }
            }

            failure = $"[Failed] No allowed method matched '{typeName}.{methodName}' with {argCount} argument(s).";
            return null;
        }

        public static bool CanConvertArguments(MethodInfo method, object[] arguments)
        {
            var parameters = method.GetParameters();
            arguments ??= Array.Empty<object>();
            if (parameters.Length != arguments.Length)
            {
                return false;
            }

            for (var index = 0; index < parameters.Length; index++)
            {
                if (!TryConvertArgument(arguments[index], parameters[index].ParameterType, out _))
                {
                    return false;
                }
            }

            return true;
        }

        public static object[] ConvertArguments(MethodInfo method, object[] arguments)
        {
            var parameters = method.GetParameters();
            arguments ??= Array.Empty<object>();
            var converted = new object[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                TryConvertArgument(arguments[index], parameters[index].ParameterType, out converted[index]);
            }

            return converted;
        }

        private static bool TryConvertArgument(object raw, Type targetType, out object converted)
        {
            converted = null;
            if (raw == null)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    return false;
                }

                return true;
            }

            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (actualType.IsInstanceOfType(raw))
            {
                converted = raw;
                return true;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(actualType))
            {
                converted = UnityObjectLocator.TryResolveObject(raw.ToString());
                return converted != null && actualType.IsInstanceOfType(converted);
            }

            try
            {
                if (actualType.IsEnum)
                {
                    converted = Enum.Parse(actualType, raw.ToString(), true);
                    return true;
                }

                converted = Convert.ChangeType(raw, actualType, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal sealed class ReflectionMethodSnapshot
    {
        public string DeclaringType;
        public string Name;
        public bool IsStatic;
        public List<object> Parameters;
        public string ReturnType;

        public static ReflectionMethodSnapshot FromMethod(MethodInfo method)
        {
            return new ReflectionMethodSnapshot
            {
                DeclaringType = method.DeclaringType?.FullName ?? string.Empty,
                Name = method.Name,
                IsStatic = method.IsStatic,
                ReturnType = method.ReturnType.FullName ?? method.ReturnType.Name,
                Parameters = method.GetParameters()
                    .Select(parameter => (object)new Dictionary<string, object>
                    {
                        ["name"] = parameter.Name,
                        ["type"] = parameter.ParameterType.FullName ?? parameter.ParameterType.Name,
                        ["hasDefaultValue"] = parameter.HasDefaultValue
                    })
                    .ToList()
            };
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["declaringType"] = DeclaringType,
                ["name"] = Name,
                ["isStatic"] = IsStatic,
                ["returnType"] = ReturnType,
                ["parameters"] = Parameters
            };
        }
    }

    internal static class ReflectionResultSerializer
    {
        public static object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is string text)
            {
                return text.Length <= 2048 ? text : text.Substring(0, 2048) + "...(truncated)";
            }

            if (value is bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
            {
                return value;
            }

            if (value is UnityEngine.Object unityObject)
            {
                return UnityObjectSnapshot.FromObject(unityObject).ToDictionary();
            }

            if (value is IEnumerable<object> enumerable)
            {
                return enumerable.Take(20).Select(Serialize).ToList();
            }

            if (value is System.Collections.IEnumerable nonGeneric && value is not string)
            {
                var items = new List<object>();
                foreach (var item in nonGeneric)
                {
                    items.Add(Serialize(item));
                    if (items.Count >= 20)
                    {
                        break;
                    }
                }

                return items;
            }

            var serialized = value.ToString();
            if (string.IsNullOrEmpty(serialized))
            {
                return serialized;
            }

            return serialized.Length <= 2048 ? serialized : serialized.Substring(0, 2048) + "...(truncated)";
        }
    }
}
