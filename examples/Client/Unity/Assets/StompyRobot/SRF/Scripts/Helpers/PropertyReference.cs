namespace SRF.Helpers
{
    using System;
    using System.Linq;
    using System.Reflection;

    public class PropertyReference
    {
        private readonly PropertyInfo _property;
        private readonly object _target;

        public PropertyReference(object target, PropertyInfo property)
        {
            SRDebugUtil.AssertNotNull(target);

            _target = target;
            _property = property;
        }

        public string PropertyName
        {
            get { return _property.Name; }
        }

        public Type PropertyType
        {
            get { return _property.PropertyType; }
        }

        public bool CanRead
        {
            get
            {
#if NETFX_CORE
				return _property.GetMethod != null && _property.GetMethod.IsPublic;
#else
                return _property.GetGetMethod() != null;
#endif
            }
        }

        public bool CanWrite
        {
            get
            {
#if NETFX_CORE
				return _property.SetMethod != null && _property.SetMethod.IsPublic;
#else
                return _property.GetSetMethod() != null;
#endif
            }
        }

        public object GetValue()
        {
            if (_property.CanRead)
            {
                return SRReflection.GetPropertyValue(_target, _property);
            }

            return null;
        }

        public void SetValue(object value)
        {
            if (_property.CanWrite)
            {
                SRReflection.SetPropertyValue(_target, _property, value);
            }
            else
            {
                throw new InvalidOperationException("Can not write to property");
            }
        }

        public T GetAttribute<T>() where T : Attribute
        {
            var attributes = _property.GetCustomAttributes(typeof (T), true).FirstOrDefault();

            return attributes as T;
        }
    }
}
