namespace Fantasy
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