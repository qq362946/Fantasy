#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Async
{
    public interface IFTask
    {
        public FTaskType FTaskType { get; set; }
        public object UserToKen { get; set; }
    }

    public enum FTaskType : byte
    {
        Task,
        UserToKenTask,
        ContagionUserToKen
    }
}