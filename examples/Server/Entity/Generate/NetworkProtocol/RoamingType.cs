using System.Collections.Generic;
namespace Fantasy
{
	// Roaming协议定义(需要定义10000以上、因为10000以内的框架预留)	
	public static class RoamingType
	{
		public const int MapRoamingType = 10001;
		public const int ChatRoamingType = 10002;
		public static IEnumerable<int> RoamingTypes
		{
			get
			{
				yield return 10001;
				yield return 10002;
			}
		}
	}
}
