using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Customers;
using OrderManagement.Application.Customers.Requests;
using OrderManagement.Application.Customers.Responses;


namespace OrderManagement.Api
{
    [ApiController]
    [Route("api/customers")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken ct)
        {
            var customer = await _customerService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var customer = await _customerService.GetByIdAsync(id, ct);
            return Ok(customer);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<CustomerResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var customers = await _customerService.GetAllAsync(ct);
            return Ok(customers);
        }
    }
}
