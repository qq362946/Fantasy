namespace SRF.Service
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ServiceAttribute : Attribute
    {
        public ServiceAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ServiceSelectorAttribute : Attribute
    {
        public ServiceSelectorAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ServiceConstructorAttribute : Attribute
    {
        public ServiceConstructorAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; private set; }
    }
}
