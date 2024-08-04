// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System.Collections.Generic;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy
{
    public sealed class CoroutineLockQueuePool : PoolCore<CoroutineLockQueue>
    {
        public CoroutineLockQueuePool() : base(2000) { }
    }
    
    public sealed class CoroutineLockQueue : Queue<WaitCoroutineLock>, IPool
    {
        public bool IsPool { get; set; }
    }
}