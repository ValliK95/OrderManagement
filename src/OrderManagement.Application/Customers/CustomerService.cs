using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Customers.Requests;
using OrderManagement.Application.Customers.Responses;
using OrderManagement.Domain.Entities;


namespace OrderManagement.Application.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CreateCustomerRequest> _validator;

        public CustomerService(IAppDbContext db, IValidator<CreateCustomerRequest> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
        {
            await _validator.ValidateAndThrowAsync(request, ct);

            var email = request.Email.Trim().ToLowerInvariant();

            if (await _db.Customers.AnyAsync(c => c.Email == email, ct))
                throw new ConflictException($"A customer with email '{email}' already exists.");

            var customer = new Customer
            {
                FullName = request.FullName.Trim(),
                Email = email,
                CreatedDate = DateTime.UtcNow
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(ct);

            return new CustomerResponse(customer.Id, customer.FullName, customer.Email, customer.CreatedDate);
        }

        public async Task<CustomerResponse> GetByIdAsync(int id, CancellationToken ct)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CustomerResponse(c.Id, c.FullName, c.Email, c.CreatedDate))
                .FirstOrDefaultAsync(ct);

            return customer ?? throw new NotFoundException($"Customer with id {id} was not found.");
        }

        public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken ct)
        {
            return await _db.Customers
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => new CustomerResponse(c.Id, c.FullName, c.Email, c.CreatedDate))
                .ToListAsync(ct);
        }
    }
}
