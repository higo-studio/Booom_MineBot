using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Threading.Tasks;

namespace McpBridge.Editor
{
    internal static class McpToolRegistry
    {
        private static readonly object s_Gate = new();
        private static List<RegisteredTool> s_Tools;

        public static List<ToolDescriptor> GetAllDescriptors()
        {
            lock (s_Gate)
            {
                EnsureLoaded();
                return s_Tools.Select(tool => tool.Descriptor).ToList();
            }
        }

        public static List<ToolDescriptor> GetEnabledDescriptors()
        {
            var settings = McpBridgeSettings.instance;
            return GetAllDescriptors().FindAll(descriptor => settings.IsToolEnabled(descriptor.Name));
        }

        public static async Task<ToolCallResult> InvokeAsync(string toolName, Dictionary<string, object> arguments)
        {
            RegisteredTool tool;
            lock (s_Gate)
            {
                EnsureLoaded();
                tool = s_Tools.FirstOrDefault(candidate => candidate.Descriptor.Name == toolName);
            }

            if (tool == null) throw new InvalidOperationException($"Tool '{toolName}' is not registered.");

            var instance = tool.Method.IsStatic ? null : Activator.CreateInstance(tool.DeclaringType);
            var parameters = BindArguments(tool, arguments);
            var rawResult = tool.Method.Invoke(instance, parameters);
            rawResult = await AwaitIfNeededAsync(rawResult).ConfigureAwait(false);

            if (rawResult is ToolCallResult typedResult) return typedResult;
            if (rawResult is string text)
            {
                return new ToolCallResult
                {
                    Text = text,
                    StructuredContent = new Dictionary<string, object> { ["ok"] = true, ["text"] = text }
                };
            }
            return new ToolCallResult
            {
                Text = McpBridgeJson.Serialize(rawResult),
                StructuredContent = rawResult
            };
        }

        public static void Invalidate()
        {
            lock (s_Gate) { s_Tools = null; }
        }

        private static void EnsureLoaded()
        {
            if (s_Tools != null) return;

            s_Tools = new List<RegisteredTool>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException exception) { types = exception.Types.Where(type => type != null).ToArray(); }

                foreach (var type in types)
                {
                    if (type == null || type.GetCustomAttribute<McpPluginToolTypeAttribute>() == null) continue;
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    {
                        var toolAttribute = method.GetCustomAttribute<McpPluginToolAttribute>();
                        if (toolAttribute == null) continue;

                        s_Tools.Add(new RegisteredTool(type, method, new ToolDescriptor
                        {
                            Name = toolAttribute.Name,
                            Title = string.IsNullOrWhiteSpace(toolAttribute.Title) ? toolAttribute.Name : toolAttribute.Title,
                            Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                            InputSchema = BuildSchema(method)
                        }));
                    }
                }
            }

            s_Tools.Add(new RegisteredTool(null, null, new ToolDescriptor
            {
                Name = McpCompileTracker.ToolName,
                Title = "Compile Unity Scripts",
                Description = "Triggers Unity script compilation and waits inline until the editor reports success or errors. If Unity is in Play Mode, it returns a blocked status unless exitPlayMode is true.",
                InputSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["exitPlayMode"] = new Dictionary<string, object>
                        {
                            ["type"] = "boolean",
                            ["description"] = "When true, exits the current Play Mode session before compiling."
                        }
                    },
                    ["required"] = Array.Empty<string>()
                }
            }));
        }

        private static object[] BindArguments(RegisteredTool tool, IReadOnlyDictionary<string, object> arguments)
        {
            var bound = new object[tool.Parameters.Length];
            for (var index = 0; index < tool.Parameters.Length; index++)
            {
                var parameter = tool.Parameters[index];
                if (!arguments.TryGetValue(parameter.Name, out var rawValue))
                {
                    bound[index] = parameter.HasDefaultValue ? parameter.DefaultValue : GetDefault(parameter.ParameterType);
                    continue;
                }
                bound[index] = ConvertArgument(rawValue, parameter.ParameterType);
            }
            return bound;
        }

        private static async Task<object> AwaitIfNeededAsync(object value)
        {
            if (value is not Task task) return value;
            await task.ConfigureAwait(false);
            var taskType = task.GetType();
            return taskType.IsGenericType ? taskType.GetProperty("Result")?.GetValue(task) : null;
        }

        private static object BuildSchema(MethodInfo method)
        {
            var properties = new Dictionary<string, object>();
            var required = new List<string>();
            foreach (var parameter in method.GetParameters())
            {
                properties[parameter.Name] = BuildParameterSchema(parameter);
                if (!parameter.HasDefaultValue && !IsNullable(parameter.ParameterType)) required.Add(parameter.Name);
            }
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required
            };
        }

        private static object BuildParameterSchema(ParameterInfo parameter)
        {
            var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            var schema = new Dictionary<string, object>();
            var description = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrWhiteSpace(description)) schema["description"] = description;

            if (parameterType == typeof(string)) schema["type"] = "string";
            else if (parameterType == typeof(bool)) schema["type"] = "boolean";
            else if (parameterType == typeof(int) || parameterType == typeof(long)) schema["type"] = "integer";
            else if (parameterType == typeof(float) || parameterType == typeof(double) || parameterType == typeof(decimal)) schema["type"] = "number";
            else if (parameterType.IsEnum)
            {
                schema["type"] = "string";
                schema["enum"] = Enum.GetNames(parameterType);
            }
            else if (parameterType.IsArray || (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                schema["type"] = "array";
            }
            else schema["type"] = "object";
            return schema;
        }

        private static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

        private static object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        private static object ConvertArgument(object rawValue, Type targetType)
        {
            if (rawValue == null) return GetDefault(targetType);
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (actualType.IsInstanceOfType(rawValue)) return rawValue;
            if (actualType.IsArray)
            {
                return ConvertArrayArgument(rawValue, actualType);
            }
            if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ConvertListArgument(rawValue, actualType);
            }
            if (actualType == typeof(string)) return rawValue.ToString();
            if (actualType == typeof(bool)) return rawValue is bool boolValue ? boolValue : bool.Parse(rawValue.ToString());
            if (actualType.IsEnum) return Enum.Parse(actualType, rawValue.ToString(), true);
            if (rawValue is Dictionary<string, object> dictionary && actualType == typeof(Dictionary<string, object>))
            {
                return dictionary;
            }
            if (rawValue is IConvertible) return Convert.ChangeType(rawValue, actualType, CultureInfo.InvariantCulture);
            return rawValue;
        }

        private static object ConvertArrayArgument(object rawValue, Type arrayType)
        {
            if (rawValue is not System.Collections.IEnumerable enumerable || rawValue is string)
            {
                return GetDefault(arrayType);
            }

            var elementType = arrayType.GetElementType() ?? typeof(object);
            var items = enumerable.Cast<object>()
                .Select(item => ConvertArgument(item, elementType))
                .ToArray();
            var array = Array.CreateInstance(elementType, items.Length);
            for (var index = 0; index < items.Length; index++)
            {
                array.SetValue(items[index], index);
            }

            return array;
        }

        private static object ConvertListArgument(object rawValue, Type listType)
        {
            if (rawValue is not System.Collections.IEnumerable enumerable || rawValue is string)
            {
                return Activator.CreateInstance(listType);
            }

            var elementType = listType.GetGenericArguments()[0];
            var list = (System.Collections.IList)Activator.CreateInstance(listType);
            foreach (var item in enumerable.Cast<object>())
            {
                list.Add(ConvertArgument(item, elementType));
            }

            return list;
        }

        private sealed class RegisteredTool
        {
            public RegisteredTool(Type declaringType, MethodInfo method, ToolDescriptor descriptor)
            {
                DeclaringType = declaringType;
                Method = method;
                Descriptor = descriptor;
                Parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();
            }

            public Type DeclaringType { get; }
            public MethodInfo Method { get; }
            public ToolDescriptor Descriptor { get; }
            public ParameterInfo[] Parameters { get; }
        }
    }
}
