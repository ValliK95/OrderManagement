namespace OrderManagement.Application.Customers
{
    public record CustomerResponse(int Id, string FullName, string Email, DateTime CreatedDate);
}
