using OrderManagement.Domain.Enum;

namespace OrderManagement.Application.Orders.Requests
{
    public record UpdateOrderStatusRequest(OrderStatus Status);
}
