using DevOpsDemo.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRevenueByCategoryAsync()
    {
        var result = await _salesService.GetRevenueByCategoryAsync();
        return Ok(result);
    }
}
