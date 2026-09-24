using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Customer> Customers { get; }
        DbSet<Product> Products { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
