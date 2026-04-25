using System;

namespace McpBridge
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class McpPluginToolTypeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class McpPluginToolAttribute : Attribute
    {
        public McpPluginToolAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Title { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
    public sealed class McpReflectionAllowedAttribute : Attribute
    {
    }
}
