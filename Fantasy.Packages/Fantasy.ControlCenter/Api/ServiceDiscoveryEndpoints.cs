using Fantasy.ControlCenter.Infrastructure;

namespace Fantasy.ControlCenter.Api;

internal static class ServiceDiscoveryEndpoints
{
    public static void MapServiceDiscoveryEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/instances", (ControlCenterStore store, CancellationToken cancellationToken) =>
            store.GetInstancesAsync(cancellationToken));

        api.MapPost("/instances/register", (RegisterInstanceRequest request, ControlCenterStore store) =>
        {
            try
            {
                return store.RegisterInstance(request)
                    ? Results.Ok(new { request.InstanceId, Status = "Registered" })
                    : Results.BadRequest(new { Message = $"Scene {request.SceneId} 不存在或未启用。" });
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });
        
        api.MapPost(
            "/instances/sub-scenes/register",
            (RegisterSubSceneRequest request, ControlCenterStore store) =>
            {
                try
                {
                    return store.RegisterSubScene(request)
                        ? Results.Ok(new
                        {
                            request.InstanceId,
                            request.ParentInstanceId,
                            Status = "Registered"
                        })
                        : Results.NotFound(new
                        {
                            Message =
                                $"父 Root Scene 实例 {request.ParentInstanceId} 不存在或已经离线。"
                        });
                }
                catch (Exception exception)
                    when (exception is ArgumentException or InvalidOperationException)
                {
                    return Results.Problem(
                        exception.Message,
                        statusCode: StatusCodes.Status400BadRequest);
                }
            });

        api.MapPost("/instances/heartbeat", (HeartbeatRequest request, ControlCenterStore store) =>
        {
            try
            {
                return store.Heartbeat(request)
                    ? Results.Ok(new { request.InstanceId, Status = "Online" })
                    : Results.NotFound(new { Message = $"实例 {request.InstanceId} 尚未注册。" });
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });

        api.MapPost("/instances/heartbeat/batch", (BatchHeartbeatRequest request, ControlCenterStore store) =>
        {
            try
            {
                return Results.Ok(store.Heartbeat(request));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { exception.Message });
            }
        });

        api.MapPost("/instances/{instanceId}/offline", (string instanceId, ControlCenterStore store) =>
            store.SetInstanceOffline(instanceId)
                ? Results.Ok(new { InstanceId = instanceId, Status = "Offline" })
                : Results.NotFound());

        api.MapGet("/discovery/scenes", (
            string? sceneType,
            uint? namespaceId,
            uint? worldId,
            uint? worldGroupId,
            ControlCenterStore store) =>
        {
            if (string.IsNullOrWhiteSpace(sceneType))
            {
                return Results.BadRequest(new { Message = "sceneType 是必填参数。" });
            }

            if (!namespaceId.HasValue || namespaceId == 0)
            {
                return Results.BadRequest(new { Message = "namespaceId 是必填参数，并且必须大于 0。" });
            }

            if (worldId.HasValue && worldGroupId.HasValue)
            {
                return Results.BadRequest(new { Message = "worldId 和 worldGroupId 不能同时指定。" });
            }

            if (worldId == 0 || worldGroupId == 0)
            {
                return Results.BadRequest(new { Message = "查询范围 ID 必须大于 0。" });
            }

            return Results.Ok(store.Discover(sceneType, namespaceId.Value, worldId, worldGroupId));
        });
        
        api.MapGet(
            "/discovery/scenes/{sceneId:min(1)}",
            (
                uint sceneId,
                uint? namespaceId,
                ControlCenterStore store
            ) =>
            {
                if (!namespaceId.HasValue || namespaceId.Value == 0)
                {
                    return Results.BadRequest(new
                    {
                        Message =
                            "namespaceId 是必填参数，并且必须大于 0。"
                    });
                }

                return Results.Ok(
                    store.ResolveScene(
                        sceneId,
                        namespaceId.Value));
            });
        
        api.MapGet("/discovery/sub-scenes", (
            long? parentAddress,
            string? sceneType,
            ControlCenterStore store) =>
        {
            if (!parentAddress.HasValue || parentAddress == 0)
            {
                return Results.BadRequest(new
                {
                    Message = "parentAddress 是必填参数，并且不能为 0。"
                });
            }

            if (string.IsNullOrWhiteSpace(sceneType))
            {
                return Results.BadRequest(new
                {
                    Message = "sceneType 是必填参数。"
                });
            }

            return Results.Ok(
                store.DiscoverSubScenes(
                    parentAddress.Value,
                    sceneType));
        });
    }
}
