using DevOpsDemo.Application.DTOs;
using DevOpsDemo.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("GetPagedWithCount")]
    public async Task<IActionResult> GetPagedWithCount([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetPagedWithCountAsync(page, pageSize);
        return Ok(new { totalCount = result.Item2, data = result.Item1 });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var product = await _service.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDto productDto)
    {
        var created = await _service.CreateAsync(productDto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ProductDto productDto)
    {
        if (id != productDto.Id) return BadRequest("Id mismatch");
        await _service.UpdateAsync(productDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? category, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? searchText)
    {
        var results = await _service.SearchByFilterAsync(category, minPrice, maxPrice, searchText);
        return Ok(results);
    }

    [HttpGet("aggregations")]
    public async Task<IActionResult> Aggregations()
    {
        var result = await _service.GetAggregationsAsync();
        return Ok(result);
    }
}
