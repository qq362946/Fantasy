using Fantasy.ControlCenter.Infrastructure;
using Fantasy.ControlCenter.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.ControlCenter.Controllers;

[ApiController]
[Route("api/v1/world-groups")]
public sealed class WorldGroupsController(ControlCenterStore store) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<WorldGroupDefinition>> Get(CancellationToken cancellationToken) =>
        store.GetWorldGroupsAsync(cancellationToken);

    [HttpPut("{id:min(1)}")]
    public async Task<IActionResult> Save(
        uint id,
        WorldGroupDefinition model,
        CancellationToken cancellationToken)
    {
        model.Id = id;
        await store.SaveWorldGroupAsync(model, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:min(1)}")]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        await store.DeleteWorldGroupAsync(id, cancellationToken);
        return NoContent();
    }
}
