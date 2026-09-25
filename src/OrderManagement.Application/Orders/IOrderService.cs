using OrderManagement.Application.Orders.Requests;
using OrderManagement.Application.Orders.Responses;

namespace OrderManagement.Application.Orders
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct);
        Task<OrderResponse> GetByIdAsync(int id, CancellationToken ct);
        Task<OrderResponse> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken ct);
    }
}
