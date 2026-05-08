using System;

namespace Minebot.Bootstrap
{
    public enum MinebotRuntimeTag
    {
        Provider,
        Consumer
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MinebotRuntimeTagAttribute : Attribute
    {
        public MinebotRuntimeTagAttribute(MinebotRuntimeTag tag)
        {
            Tag = tag;
        }

        public MinebotRuntimeTag Tag { get; }
    }
}
