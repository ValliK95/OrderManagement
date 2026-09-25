using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Products;
using OrderManagement.Application.Products.Requests;
using OrderManagement.Application.Products.Responses;

namespace OrderManagement.Api
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct)
        {
            var product = await _productService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }


        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var product = await _productService.GetByIdAsync(id, ct);
            return Ok(product);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? name, CancellationToken ct)
        {
            var products = await _productService.GetAllAsync(name, ct);
            return Ok(products);
        }

        [HttpPatch("{id:int}/stock")]
        [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateStock(int id, UpdateStockRequest request, CancellationToken ct)
        {
            var product = await _productService.UpdateStockAsync(id, request, ct);
            return Ok(product);
        }
    }
}
