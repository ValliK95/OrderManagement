namespace OrderManagement.Application.Products.Responses
{
    public record ProductResponse(int Id, string Name, string Sku, decimal Price, int StockQuantity);
}
