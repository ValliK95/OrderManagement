namespace OrderManagement.Application.Orders.Requests
{
    public record CreateOrderRequest(int CustomerId, List<CreateOrderItemRequest> Items);

    public record CreateOrderItemRequest(int ProductId, int Quantity);
}
