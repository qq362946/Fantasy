using System.Runtime.InteropServices;

namespace Fantasy.Helper
{
    /// <summary>
    /// 表示一个路由 ID 的结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RouteIdStruct
    {
        // +----------------------+---------------------------+
        // | AppId(8) 最多255个进程 | WordId(10) 最多1023个世界 
        // +----------------------+---------------------------+

        /// <summary>
        /// 进程 ID
        /// </summary>
        public ushort AppId;
        /// <summary>
        /// 世界 ID
        /// </summary>
        public ushort WordId;

        /// <summary>
        /// AppId 的掩码。
        /// </summary>
        public const int MaskAppId = 0xFF;
        /// <summary>
        /// WordId 的掩码。
        /// </summary>
        public const int MaskWordId = 0x3FF;

        /// <summary>
        /// 初始化一个新的路由 ID 结构。
        /// </summary>
        /// <param name="appId">进程 ID。</param>
        /// <param name="wordId">世界 ID。</param>
        public RouteIdStruct(ushort appId, ushort wordId)
        {
            AppId = appId;
            WordId = wordId;
        }

        /// <summary>
        /// 将路由 ID 结构隐式转换为无符号整型。
        /// </summary>
        /// <param name="routeId">要转换的路由 ID 结构。</param>
        public static implicit operator uint(RouteIdStruct routeId)
        {
            uint result = 0;
            result |= routeId.WordId;
            result |= (uint)routeId.AppId << 10;
            return result;
        }

        /// <summary>
        /// 将无符号整型隐式转换为路由 ID 结构。
        /// </summary>
        /// <param name="routeId">要转换的无符号整型路由 ID。</param>
        public static implicit operator RouteIdStruct(uint routeId)
        {
            var idStruct = new RouteIdStruct()
            {
                WordId = (ushort)(routeId & MaskWordId)
            };

            routeId >>= 10;
            idStruct.AppId = (ushort)(routeId & MaskAppId);
            return idStruct;
        }
    }
}