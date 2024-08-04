namespace Fantasy;

public sealed class AuthenticationComponentAwakeSystem : AwakeSystem<AuthenticationComponent>
{
    protected override void Awake(AuthenticationComponent self)
    {
        Log.Debug($"AuthenticationComponent:{Thread.CurrentThread.ManagedThreadId}");
    }
}