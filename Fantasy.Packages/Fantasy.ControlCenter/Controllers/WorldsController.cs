using Fantasy.ControlCenter.Infrastructure;
using Fantasy.ControlCenter.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.ControlCenter.Controllers;

[ApiController]
[Route("api/v1/worlds")]
public sealed class WorldsController(ControlCenterStore store) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<WorldDefinition>> Get(CancellationToken cancellationToken) =>
        store.GetWorldsAsync(cancellationToken);

    [HttpPut("{id:min(1)}")]
    public async Task<IActionResult> Save(
        uint id,
        WorldDefinition model,
        CancellationToken cancellationToken)
    {
        model.Id = id;
        await store.SaveWorldAsync(model, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:min(1)}")]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        await store.DeleteWorldAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{worldId:min(1)}/databases")]
    public Task<IReadOnlyList<DatabaseDefinition>> GetDatabases(
        uint worldId,
        CancellationToken cancellationToken) =>
        store.GetDatabasesAsync(worldId, cancellationToken);

    [HttpPost("{worldId:min(1)}/databases")]
    public Task<IActionResult> CreateDatabase(
        uint worldId,
        DatabaseDefinition model,
        CancellationToken cancellationToken) =>
        SaveDatabase(0, worldId, model, cancellationToken);

    [HttpPut("{worldId:min(1)}/databases/{id:long:min(1)}")]
    public Task<IActionResult> UpdateDatabase(
        uint worldId,
        long id,
        DatabaseDefinition model,
        CancellationToken cancellationToken) =>
        SaveDatabase(id, worldId, model, cancellationToken);

    [HttpDelete("{worldId:min(1)}/databases/{id:long:min(1)}")]
    public async Task<IActionResult> DeleteDatabase(
        uint worldId,
        long id,
        CancellationToken cancellationToken)
    {
        await store.DeleteDatabaseAsync(id, worldId, cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> SaveDatabase(
        long id,
        uint worldId,
        DatabaseDefinition model,
        CancellationToken cancellationToken)
    {
        model.Id = id;
        model.WorldId = worldId;
        var savedId = await store.SaveDatabaseAsync(model, cancellationToken);
        return Ok(new { Id = savedId, WorldId = worldId });
    }
}
