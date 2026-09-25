using OrderManagement.Application.Products.Requests;
using OrderManagement.Application.Products.Responses;

namespace OrderManagement.Application.Products
{
    public interface IProductService
    {
        Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct);
        Task<ProductResponse> GetByIdAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<ProductResponse>> GetAllAsync(string? name, CancellationToken ct);
        Task<ProductResponse> UpdateStockAsync(int id, UpdateStockRequest request, CancellationToken ct);
    }
}
