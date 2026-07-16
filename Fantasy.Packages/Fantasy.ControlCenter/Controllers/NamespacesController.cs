using Fantasy.ControlCenter.Infrastructure;
using Fantasy.ControlCenter.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.ControlCenter.Controllers;

[ApiController]
[Route("api/v1/namespaces")]
public sealed class NamespacesController(ControlCenterStore store) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<NamespaceDefinition>> Get(CancellationToken cancellationToken) =>
        store.GetNamespacesAsync(cancellationToken);

    [HttpPut("{id:min(1)}")]
    public async Task<IActionResult> Save(
        uint id,
        NamespaceDefinition model,
        CancellationToken cancellationToken)
    {
        model.Id = id;
        await store.SaveNamespaceAsync(model, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:min(1)}")]
    public async Task<IActionResult> Delete(uint id, CancellationToken cancellationToken)
    {
        await store.DeleteNamespaceAsync(id, cancellationToken);
        return NoContent();
    }
}
