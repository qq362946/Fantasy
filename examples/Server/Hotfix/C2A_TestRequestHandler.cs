namespace Fantasy;

public class C2A_TestRequestHandler : MessageRPC<C2A_TestRequest, A2C_TestResponse>
{
    protected override async FTask Run(Session session, C2A_TestRequest request, A2C_TestResponse response, Action reply)
    {
        Log.Debug("C2A_TestRequestHandler");
        await FTask.CompletedTask;
    }
}