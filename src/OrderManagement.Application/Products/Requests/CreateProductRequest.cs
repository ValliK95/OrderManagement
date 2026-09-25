namespace OrderManagement.Application.Products.Requests
{
    public record CreateProductRequest(string Name, string Sku, decimal Price, int StockQuantity);
}
