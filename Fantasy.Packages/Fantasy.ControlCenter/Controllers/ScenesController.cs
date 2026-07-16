using Fantasy.ControlCenter.Infrastructure;
using Fantasy.ControlCenter.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.ControlCenter.Controllers;

[ApiController]
[Route("api/v1/scenes")]
public sealed class ScenesController(ControlCenterStore store) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SceneDefinition>> Get(CancellationToken cancellationToken) =>
        store.GetScenesAsync(cancellationToken);

    [HttpPut("{id:min(1)}")]
    public async Task<IActionResult> Save(
        uint id,
        SceneDefinition model,
        CancellationToken cancellationToken)
    {
        model.Id = id;
        await store.SaveSceneAsync(model, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:min(1)}")]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        await store.DeleteSceneAsync(id, cancellationToken);
        return NoContent();
    }
}
