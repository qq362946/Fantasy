using Fantasy.ControlCenter.Infrastructure;

namespace Fantasy.ControlCenter.Api;

internal static class SystemEndpoints
{
    public static void MapSystemEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/health", (ControlCenterStore store) =>
            Results.Ok(new
            {
                Status = "Healthy",
                Revision = store.GetRevision(),
                TimeUtc = DateTimeOffset.UtcNow
            }));

        api.MapGet("/summary", (ControlCenterStore store) => store.GetSummary());
        api.MapGet("/runtime/config", (uint? processId, ControlCenterStore store) =>
        {
            if (!processId.HasValue)
            {
                return Results.Ok(store.GetRuntimeConfig());
            }

            if (processId.Value == 0)
            {
                return Results.BadRequest(new { Message = "processId 必须大于 0。" });
            }

            return store.TryGetRuntimeConfig(processId.Value, out var runtimeConfig)
                ? Results.Ok(runtimeConfig)
                : Results.NotFound(new { Message = $"Process {processId.Value} 不存在、未启用或没有启用的 Scene。" });
        });
    }
}
