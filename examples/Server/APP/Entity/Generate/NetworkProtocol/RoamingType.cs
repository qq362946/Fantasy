using System.Collections.Generic;
namespace Fantasy
{
    /// <summary>
    /// 本代码有编辑器生成,请不要再这里进行编辑。
    /// Roaming协议定义(需要定义10000以上、因为10000以内的框架预留)
    /// </summary>
    public static partial class RoamingType
    {
        public const int MapRoamingType = 10001; // Map
        public const int ChatRoamingType = 10002; // Chat

        public static IEnumerable<int> RoamingTypes
        {
            get
            {
                yield return MapRoamingType;
                yield return ChatRoamingType;
            }
        }
    }
}