namespace SRF.Helpers
{
    using System;
    using System.Reflection;

    public static class SRReflection
    {
        public static void SetPropertyValue(object obj, PropertyInfo p, object value)
        {
#if NETFX_CORE
			p.SetValue(obj, value, null);
#else
            p.GetSetMethod().Invoke(obj, new[] {value});
#endif
        }

        public static object GetPropertyValue(object obj, PropertyInfo p)
        {
#if NETFX_CORE
			return p.GetValue(obj, null);
#else
            return p.GetGetMethod().Invoke(obj, null);
#endif
        }

        public static T GetAttribute<T>(MemberInfo t) where T : Attribute
        {
#if !NETFX_CORE
            return Attribute.GetCustomAttribute(t, typeof (T)) as T;
#else
			return t.GetCustomAttribute(typeof (T), true) as T;
#endif
        }

#if NETFX_CORE

		public static T GetAttribute<T>(Type t) where T : Attribute
		{
			
			return GetAttribute<T>(t.GetTypeInfo());

		}

#endif
    }
}
