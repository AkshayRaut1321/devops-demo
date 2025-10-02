using DevOpsDemo.Application.DTOs;
using DevOpsDemo.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductAndDiscountService _productAndDiscountService;

    public ProductsController(IProductService productService, IProductAndDiscountService productAndDiscountService)
    {
        _productService = productService;
        _productAndDiscountService = productAndDiscountService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _productService.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("GetPagedWithCount")]
    public async Task<IActionResult> GetPagedWithCount([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _productService.GetPagedWithCountAsync(page, pageSize);
        return Ok(new { totalCount = result.Item2, data = result.Item1 });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDto productDto)
    {
        var created = await _productService.CreateAsync(productDto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ProductDto productDto)
    {
        if (id != productDto.Id) return BadRequest("Id mismatch");
        await _productService.UpdateAsync(productDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? category, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? searchText)
    {
        var results = await _productService.SearchByFilterAsync(category, minPrice, maxPrice, searchText);
        return Ok(results);
    }

    [HttpGet("aggregations")]
    public async Task<IActionResult> Aggregations()
    {
        var result = await _productService.GetAggregationsAsync();
        return Ok(result);
    }

    [HttpGet("GetProductsAndDiscounts")]
    public async Task<IActionResult> GetProductsAndDiscounts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _productAndDiscountService.GetPagedAsync(page, pageSize);
        return Ok(result);
    }
}
