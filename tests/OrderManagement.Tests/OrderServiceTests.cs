using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Application.Orders;
using OrderManagement.Application.Orders.Requests;
using OrderManagement.Application.Orders.Validators;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enum;
using OrderManagement.Infrastructure;

namespace OrderManagement.Tests
{
    public class OrderServiceTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private static OrderService CreateService(AppDbContext db) =>
            new OrderService(db, new CreateOrderValidator(), new UpdateOrderStatusValidator());

        private static async Task<(Customer customer, Product product)> SeedAsync(AppDbContext db)
        {
            var customer = new Customer { FullName = "Test Customer", Email = "test@test.com", CreatedDate = DateTime.UtcNow };
            var product = new Product { Name = "Earphone", Sku = "EP-01", Price = 100, StockQuantity = 10 };

            db.Customers.Add(customer);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            return (customer, product);
        }

        private static CreateOrderRequest OrderOf(int customerId, int productId, int quantity) =>
            new(customerId, new List<CreateOrderItemRequest> { new(productId, quantity) });

        [Fact]
        public async Task CreateOrder_ReducesStock_AndStartsAsPending()
        {
            var db = CreateDb();
            var (customer, product) = await SeedAsync(db);
            var service = CreateService(db);

            var result = await service.CreateAsync(OrderOf(customer.Id, product.Id, 3), CancellationToken.None);

            Assert.Equal(OrderStatus.Pending, result.Status);
            Assert.Equal(300, result.TotalAmount);
            Assert.Equal(7, product.StockQuantity);
        }

        [Fact]
        public async Task CreateOrder_WithInsufficientStock_ThrowsConflict_AndStockIsUnchanged()
        {
            var db = CreateDb();
            var (customer, product) = await SeedAsync(db);
            var service = CreateService(db);

            await Assert.ThrowsAsync<ConflictException>(() =>
                service.CreateAsync(OrderOf(customer.Id, product.Id, 20), CancellationToken.None));

            Assert.Equal(10, product.StockQuantity);
            Assert.Empty(db.Orders);
        }

        [Fact]
        public async Task CreateOrder_WithUnknownCustomer_ThrowsNotFound()
        {
            var db = CreateDb();
            var (_, product) = await SeedAsync(db);
            var service = CreateService(db);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.CreateAsync(OrderOf(999, product.Id, 1), CancellationToken.None));
        }

        [Fact]
        public async Task CancelOrder_ReturnsItemsToStock()
        {
            var db = CreateDb();
            var (customer, product) = await SeedAsync(db);
            var service = CreateService(db);

            var order = await service.CreateAsync(OrderOf(customer.Id, product.Id, 3), CancellationToken.None);
            Assert.Equal(7, product.StockQuantity);

            var result = await service.UpdateStatusAsync(
                order.Id, new UpdateOrderStatusRequest(OrderStatus.Cancelled), CancellationToken.None);

            Assert.Equal(OrderStatus.Cancelled, result.Status);
            Assert.Equal(10, product.StockQuantity);
        }
    }
}
