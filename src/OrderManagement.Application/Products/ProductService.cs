using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Products.Requests;
using OrderManagement.Application.Products.Responses;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Products
{
    public class ProductService : IProductService
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CreateProductRequest> _createValidator;
        private readonly IValidator<UpdateStockRequest> _stockValidator;

        public ProductService(
            IAppDbContext db,
            IValidator<CreateProductRequest> createValidator,
            IValidator<UpdateStockRequest> stockValidator)
        {
            _db = db;
            _createValidator = createValidator;
            _stockValidator = stockValidator;
        }

        public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct)
        {
            await _createValidator.ValidateAndThrowAsync(request, ct);

            var sku = request.Sku.Trim().ToUpperInvariant();

            if (await _db.Products.AnyAsync(p => p.Sku == sku, ct))
                throw new ConflictException($"A product with SKU '{sku}' already exists.");

            var product = new Product
            {
                Name = request.Name.Trim(),
                Sku = sku,
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(ct);

            return new ProductResponse(product.Id, product.Name, product.Sku, product.Price, product.StockQuantity);
        }

        public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken ct)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductResponse(p.Id, p.Name, p.Sku, p.Price, p.StockQuantity))
                .FirstOrDefaultAsync(ct);

            return product ?? throw new NotFoundException($"Product with id {id} was not found.");
        }

        public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(string? name, CancellationToken ct)
        {
            var query = _db.Products.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var search = name.Trim();
                query = query.Where(p => p.Name.Contains(search));
            }

            return await query
                .OrderBy(p => p.Name)
                .Select(p => new ProductResponse(p.Id, p.Name, p.Sku, p.Price, p.StockQuantity))
                .ToListAsync(ct);
        }

        public async Task<ProductResponse> UpdateStockAsync(int id, UpdateStockRequest request, CancellationToken ct)
        {
            await _stockValidator.ValidateAndThrowAsync(request, ct);

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                ?? throw new NotFoundException($"Product with id {id} was not found.");

            product.StockQuantity = request.StockQuantity;
            await _db.SaveChangesAsync(ct);

            return new ProductResponse(product.Id, product.Name, product.Sku, product.Price, product.StockQuantity);
        }
    }
}
