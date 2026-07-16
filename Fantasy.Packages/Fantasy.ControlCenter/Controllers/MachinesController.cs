using Fantasy.ControlCenter.Infrastructure;
using Fantasy.ControlCenter.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.ControlCenter.Controllers;

[ApiController]
[Route("api/v1/machines")]
public sealed class MachinesController(ControlCenterStore store) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<MachineDefinition>> Get(CancellationToken cancellationToken) =>
        store.GetMachinesAsync(cancellationToken);

    [HttpPut("{id:min(1)}")]
    public async Task<IActionResult> Save(
        uint id,
        MachineDefinition model,
        CancellationToken cancellationToken)
    {
        model.Id = id;
        await store.SaveMachineAsync(model, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:min(1)}")]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        await store.DeleteMachineAsync(id, cancellationToken);
        return NoContent();
    }
}
