using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fantasy.Helper;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.IdFactory
{
    /// <summary>
    /// 表示一个运行时的ID。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RuntimeIdStruct
    {
        // RuntimeId:23 + 8 + 8 + 25 =  64
        // +-------------------+-----------------------------+--------------------------------------+
        // |  time(23) 最大60天 | SceneId(16) 最多65535个Scene | sequence(25) 每秒每个进程能生产33554431个
        // +-------------------+-----------------------------+--------------------------------------+
        public uint Time { get; private set; }
        public uint SceneId { get; private set; }
        public uint Sequence { get; private set; }

        public const uint MaskSequence = 0x1FFFFFF;
        public const uint MaskSceneId = 0xFFFF;
        public const uint MaskTime = 0x7FFFFF;
        
        /// <summary>
        /// RuntimeIdStruct（如果超过下面参数的设定该ID会失效）。
        /// </summary>
        /// <param name="time">time不能超过8388607</param>
        /// <param name="sceneId">sceneId不能超过65535</param>
        /// <param name="sequence">sequence不能超过33554431</param>
        public RuntimeIdStruct(uint time, uint sceneId, uint sequence)
        {
            // 因为都是在配置表里拿到参数、所以这个不做边界判定、能节省一点点性能。
            Time = time;
            SceneId = sceneId;
            Sequence = sequence;
        }

        public static implicit operator long(RuntimeIdStruct runtimeIdStruct)
        {
            ulong result = runtimeIdStruct.Sequence;
            result |= (ulong)runtimeIdStruct.SceneId << 25;
            result |= (ulong)runtimeIdStruct.Time << 41;
            return (long)result;
        }

        public static implicit operator RuntimeIdStruct(long runtimeId)
        {
            var result = (ulong)runtimeId;
            var runtimeIdStruct = new RuntimeIdStruct
            {
                Sequence = (uint)(result & MaskSequence)
            };
            result >>= 25;
            runtimeIdStruct.SceneId = (byte)(result & MaskSceneId);
            result >>= 16;
            runtimeIdStruct.Time = (uint)(result & MaskTime);
            return runtimeIdStruct;
        }
    }
    
    public sealed class RuntimeIdFactory : IRuntimeIdFactory
    {
        private readonly uint _sceneId;
        
        private uint _lastTime;
        private uint _lastSequence;
        private readonly long _epochNow;
        private readonly long _epoch1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
        
        private RuntimeIdFactory() { }

        public RuntimeIdFactory(uint sceneId) : this(TimeHelper.Now, sceneId) { }

        public RuntimeIdFactory(long epochNow, uint sceneId)
        {
            switch (sceneId)
            {
                case > 65535:
                {
                    throw new NotSupportedException($"sceneId:{sceneId} cannot be greater than 255255");
                }
                default:
                {
                    _sceneId = (ushort)sceneId;
                    _epochNow = epochNow - _epoch1970;
                    break;
                }
            }
        }

        public long Create
        {
            get
            {
                var time = (uint)((TimeHelper.Now - _epochNow) / 1000);
                
                if (time > _lastTime)
                {
                    _lastTime = time;
                    _lastSequence = 0;
                }
                else if (++_lastSequence > RuntimeIdStruct.MaskSequence - 1)
                {
                    _lastTime++;
                    _lastSequence = 0;
                }

                return new RuntimeIdStruct(time, _sceneId, _lastSequence);
            }
        }
    }

    public sealed class RuntimeIdFactoryTool : IIdFactoryTool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetTime(ref long runtimeId)
        {
            var result = (ulong)runtimeId >> 41;
            return (uint)(result & RuntimeIdStruct.MaskTime);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetSceneId(ref long runtimeId)
        {
            var result = (ulong)runtimeId >> 25;
            return (uint)(result & RuntimeIdStruct.MaskSceneId);
        }

        public byte GetWorldId(ref long entityId)
        {
            throw new NotImplementedException();
        }
    }
}