using Fantasy.ControlCenter.Infrastructure;
using Fantasy.ControlCenter.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.ControlCenter.Controllers;

[ApiController]
[Route("api/v1/topology")]
public sealed class TopologyController(ControlCenterStore store) : ControllerBase
{
    [HttpGet]
    public Task<TopologySnapshot> Get(CancellationToken cancellationToken) =>
        store.GetTopologyAsync(cancellationToken);
}
