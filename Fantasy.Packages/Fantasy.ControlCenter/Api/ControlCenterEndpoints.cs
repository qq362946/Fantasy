namespace Fantasy.ControlCenter.Api;

public static class ControlCenterEndpoints
{
    public static IEndpointRouteBuilder MapControlCenterApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapSystemEndpoints();
        api.MapServiceDiscoveryEndpoints();

        return endpoints;
    }
}
