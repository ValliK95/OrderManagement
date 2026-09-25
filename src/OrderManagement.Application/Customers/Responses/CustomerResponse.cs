namespace OrderManagement.Application.Customers.Responses
{
    public record CustomerResponse(int Id, string FullName, string Email, DateTime CreatedDate);
}
