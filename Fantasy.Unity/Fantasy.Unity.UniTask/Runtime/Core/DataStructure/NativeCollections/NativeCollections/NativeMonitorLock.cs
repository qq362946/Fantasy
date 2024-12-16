using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
#endif

#if UNITY_2021_3_OR_NEWER || GODOT
using System;
using System.Threading;
#endif

#pragma warning disable CA2208
#pragma warning disable CS8604
#pragma warning disable CS8632

// ReSharper disable ALL

namespace NativeCollections
{
    /// <summary>
    ///     Native monitorLock
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeMonitorLock : IDisposable, IEquatable<NativeMonitorLock>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        private GCHandle _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="value">Value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMonitorLock(object value) => _handle = GCHandle.Alloc(value, GCHandleType.Normal);

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="type">GCHandle type</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMonitorLock(object value, GCHandleType type) => _handle = GCHandle.Alloc(value, type);

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle.IsAllocated;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeMonitorLock other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeMonitorLock nativeMonitorLock && nativeMonitorLock == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => "NativeMonitorLock";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeMonitorLock left, NativeMonitorLock right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeMonitorLock left, NativeMonitorLock right) => left._handle != right._handle;

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!_handle.IsAllocated)
                return;
            _handle.Free();
        }

        /// <summary>
        ///     Enter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter() => Monitor.Enter(_handle.Target);

        /// <summary>
        ///     Enter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter(ref bool lockTaken) => Monitor.Enter(_handle.Target, ref lockTaken);

        /// <summary>
        ///     Enter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter() => Monitor.TryEnter(_handle.Target);

        /// <summary>
        ///     Enter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryEnter(ref bool lockTaken) => Monitor.TryEnter(_handle.Target, ref lockTaken);

        /// <summary>
        ///     Enter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter(int millisecondsTimeout) => Monitor.TryEnter(_handle.Target, millisecondsTimeout);

        /// <summary>
        ///     Enter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryEnter(int millisecondsTimeout, ref bool lockTaken) => Monitor.TryEnter(_handle.Target, millisecondsTimeout, ref lockTaken);

        /// <summary>
        ///     Is entered
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntered() => Monitor.IsEntered(_handle.Target);

        /// <summary>
        ///     Wait
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int millisecondsTimeout) => Monitor.Wait(_handle.Target, millisecondsTimeout);

        /// <summary>
        ///     Pulse
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pulse() => Monitor.Pulse(_handle.Target);

        /// <summary>
        ///     Pulse all
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PulseAll() => Monitor.PulseAll(_handle.Target);

        /// <summary>
        ///     Try enter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter(TimeSpan timeout) => Monitor.TryEnter(_handle.Target, timeout);

        /// <summary>
        ///     Try enter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryEnter(TimeSpan timeout, ref bool lockTaken) => Monitor.TryEnter(_handle.Target, timeout, ref lockTaken);

        /// <summary>
        ///     Wait
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(TimeSpan timeout) => Monitor.Wait(_handle.Target, timeout);

        /// <summary>
        ///     Wait
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait() => Monitor.Wait(_handle.Target);

        /// <summary>
        ///     Wait
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int millisecondsTimeout, bool exitContext) => Monitor.Wait(_handle.Target, millisecondsTimeout, exitContext);

        /// <summary>
        ///     Wait
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(TimeSpan timeout, bool exitContext) => Monitor.Wait(_handle.Target, timeout, exitContext);

        /// <summary>
        ///     Exit
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit() => Monitor.Exit(_handle.Target);

        /// <summary>
        ///     Empty
        /// </summary>
        public static NativeMonitorLock Empty => new();
    }
}