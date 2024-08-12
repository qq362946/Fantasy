namespace Fantasy;

public sealed class C2M_TestRequestHandler : AddressableRPC<Unit, C2M_TestRequest, M2C_TestResponse>
{
    protected override async FTask Run(Unit unit, C2M_TestRequest request, M2C_TestResponse response, Action reply)
    {
        Log.Debug($"Receive C2M_TestRequest Tag = {request.Tag}");
        response.Tag = "Hello M2C_TestResponse";
        await FTask.CompletedTask;
    }
}