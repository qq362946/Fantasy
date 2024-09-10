
#if NETFX_CORE

using System;
using System.Reflection;

namespace SRF
{

	public static class NetFxExtensions 
	{


		public static bool IsAssignableFrom(this Type @this, Type t) 
		{
			
			return @this.GetTypeInfo().IsAssignableFrom(t.GetTypeInfo());

		}

		public static bool IsInstanceOfType(this Type @this, object obj)
		{

			return @this.IsAssignableFrom(obj.GetType());

		}

	}

}

#endif
