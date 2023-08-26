using System.Runtime.InteropServices;

namespace Fantasy.Helper
{
    /// <summary>
    /// 表示一个运行时 ID 的结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RuntimeIdStruct
    {
        // +----------+------------+
        // | time(32) | sequence(32)
        // +----------+------------+

        private uint Time; // 时间部分
        private uint Sequence; // 序列号部分

        /// <summary>
        /// 初始化一个新的运行时 ID 结构。
        /// </summary>
        /// <param name="time">时间部分。</param>
        /// <param name="sequence">序列号部分。</param>
        public RuntimeIdStruct(uint time, uint sequence)
        {
            Time = time;
            Sequence = sequence;
        }

        /// <summary>
        /// 将运行时 ID 结构隐式转换为长整型。
        /// </summary>
        /// <param name="runtimeId">要转换的运行时 ID 结构。</param>
        public static implicit operator long(RuntimeIdStruct runtimeId)
        {
            ulong result = 0;
            result |= runtimeId.Sequence;
            result |= (ulong) runtimeId.Time << 32;
            return (long) result;
        }

        /// <summary>
        /// 将长整型隐式转换为运行时 ID 结构。
        /// </summary>
        /// <param name="id">要转换的长整型 ID。</param>
        public static implicit operator RuntimeIdStruct(long id)
        {
            var result = (ulong) id;
            var idStruct = new RuntimeIdStruct()
            {
                Time = (uint) (result >> 32),
                Sequence = (uint) (result & 0xFFFFFFFF)
            };
            return idStruct;
        }
    }
}