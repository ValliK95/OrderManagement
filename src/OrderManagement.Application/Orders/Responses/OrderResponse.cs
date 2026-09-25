using OrderManagement.Domain.Enum;

namespace OrderManagement.Application.Orders.Responses
{
    public record OrderResponse(
    int Id,
    DateTime OrderDate,
    OrderStatus Status,
    decimal TotalAmount,
    OrderCustomerResponse Customer,
    List<OrderItemResponse> Items);

    public record OrderCustomerResponse(int Id, string FullName, string Email);

    public record OrderItemResponse(
        int Id,
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice
        );
}
