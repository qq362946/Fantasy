#if FANTASY_NET
using System.Runtime.CompilerServices;

namespace Fantasy
{
    public static class SceneLogExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogTrace(this Scene scene, string message)
        {
            Log.Trace(scene, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogTraceInfo(this Scene scene, string message)
        {
            Log.TraceInfo(scene, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug(this Scene scene, string message)
        {
            Log.Debug(scene, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogInfo(this Scene scene, string message)
        {
            Log.Info(scene, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(this Scene scene, string message)
        {
            Log.Warning(scene, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(this Scene scene, string message)
        {
            Log.Error(scene, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(this Scene scene, Exception e)
        {
            Log.Error(scene, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogTrace(this Scene scene, string message, params object[] args)
        {
            Log.Trace(scene, message, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug(this Scene scene, string message, params object[] args)
        {
            Log.Debug(scene, message, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogInfo(this Scene scene, string message, params object[] args)
        {
            Log.Info(scene, message, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(this Scene scene, string message, params object[] args)
        {
            Log.Warning(scene, message, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(this Scene scene, string message, params object[] args)
        {
            Log.Error(scene, message, args);
        }
    }
}
#endif
