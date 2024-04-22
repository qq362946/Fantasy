using System.Runtime.InteropServices;

namespace Fantasy
{
    /// <summary>
    /// 实体的唯一标识符结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EntityIdStruct
    {
        // +-------------------+----------------------+-------------------------+----------------------------------+
        // |  time(30) 最大34年 | AppId(8) 最多255个进程 | WordId(10) 最多1023个世界 | sequence(16) 每毫秒每个进程能生产65535个
        // +-------------------+----------------------+-------------------------+----------------------------------+
        // 这样算下来、每个世界都有255个进程、就算WOW的游戏一个区也用不到255个进程
        // 计算下每个世界255个进程也就是1023 * 255 = 260865个进程、完全够用了
        // 但有一个问题是游戏最大只能有1023个区了、但可以通过战区手段来解决这个问题

        /// <summary>
        /// 获取或设置实体的时间戳部分。
        /// </summary>
        public uint Time{ get; private set; }
        /// <summary>
        /// 获取或设置实体的序列号部分。
        /// </summary>
        public uint Sequence{ get; private set; }
        /// <summary>
        /// 获取或设置实体的位置 ID。
        /// </summary>
        public uint ProcessId { get; private set; }
        /// <summary>
        /// 获取实体 ID 对应的 AppId。
        /// </summary>
        public ushort AppId => (ushort)(ProcessId >> 10 & RouteIdStruct.MaskAppId);
        /// <summary>
        /// 获取实体 ID 对应的 WordId。
        /// </summary>
        public ushort WordId => (ushort)(ProcessId & RouteIdStruct.MaskWordId);
        /// <summary>
        /// 表示用于掩码的 RouteId。
        /// </summary>
        public const int MaskRouteId = 0x3FFFF;
        /// <summary>
        /// 表示用于掩码的 Sequence。
        /// </summary>
        public const int MaskSequence = 0xFFFF;

        /// <summary>
        /// 初始化 <see cref="EntityIdStruct"/> 结构的新实例。
        /// </summary>
        /// <param name="processId">ServerID。</param>
        /// <param name="time">时间戳。</param>
        /// <param name="sequence">序列号。</param>
        public EntityIdStruct(uint processId, uint time, uint sequence)
        {
            Time = time;
            Sequence = sequence;
            ProcessId = processId;
        }

        /// <summary>
        /// 将 <see cref="EntityIdStruct"/> 隐式转换为 <see cref="long"/> 类型。
        /// </summary>
        /// <param name="entityIdStruct">要转换的 <see cref="EntityIdStruct"/> 实例。</param>
        public static implicit operator long(EntityIdStruct entityIdStruct)
        {
            ulong result = 0;
            result |= entityIdStruct.Sequence;
            result |= (ulong)entityIdStruct.ProcessId << 16;
            result |= (ulong)entityIdStruct.Time << 34;
            return (long)result;
        }

        /// <summary>
        /// 将 <see cref="long"/> 类型隐式转换为 <see cref="EntityIdStruct"/>。
        /// </summary>
        /// <param name="id">要转换的 <see cref="long"/> 类型的值。</param>
        public static implicit operator EntityIdStruct(long id)
        {
            var result = (ulong) id;
            var idStruct = new EntityIdStruct()
            {
                Sequence = (uint) (result & MaskSequence)
            };
            result >>= 16;
            idStruct.ProcessId = (uint) (result & 0x3FFFF);
            result >>= 18;
            idStruct.Time = (uint) result;
            return idStruct;
        }
    }
}