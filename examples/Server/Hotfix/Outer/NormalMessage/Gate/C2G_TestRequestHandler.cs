namespace Fantasy.Gate;

public sealed class C2G_TestRequestHandler : MessageRPC<C2G_TestRequest, G2C_TestResponse>
{
    protected override async FTask Run(Session session, C2G_TestRequest request, G2C_TestResponse response, Action reply)
    {
        Log.Debug($"Receive C2G_TestRequest Tag = {request.Tag}");
        response.Tag = "Hello G2C_TestResponse";
        await FTask.CompletedTask;
    }
}