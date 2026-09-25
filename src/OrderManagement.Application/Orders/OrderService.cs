using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.Requests;
using OrderManagement.Application.Orders.Responses;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enum;

namespace OrderManagement.Application.Orders;

public class OrderService : IOrderService
{
    private readonly IAppDbContext _db;
    private readonly IValidator<CreateOrderRequest> _createValidator;
    private readonly IValidator<UpdateOrderStatusRequest> _statusValidator;

    public OrderService(
        IAppDbContext db,
        IValidator<CreateOrderRequest> createValidator,
        IValidator<UpdateOrderStatusRequest> statusValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _statusValidator = statusValidator;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
            throw new NotFoundException($"Customer with id {request.CustomerId} was not found.");

        var requestedItems = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        var productIds = requestedItems.Select(i => i.ProductId).ToList();

        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var missingIds = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
            throw new NotFoundException($"Product(s) not found: {string.Join(", ", missingIds)}.");

        var stockProblems = requestedItems
            .Where(i => products[i.ProductId].StockQuantity < i.Quantity)
            .Select(i => $"{products[i.ProductId].Name} (requested {i.Quantity}, available {products[i.ProductId].StockQuantity})")
            .ToList();
        
        if (stockProblems.Count > 0)
            throw new ConflictException($"Insufficient stock: {string.Join("; ", stockProblems)}.");

        var order = new Order()
        {
            CustomerId = request.CustomerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        foreach (var item in requestedItems)
        {
            var product = products[item.ProductId];

            product.StockQuantity -= item.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(order.Id, ct);
    }

    public async Task<OrderResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderResponse(
                o.Id,
                o.OrderDate,
                o.Status,
                o.Items.Sum(i => i.Quantity * i.UnitPrice),
                new OrderCustomerResponse(o.Customer.Id, o.Customer.FullName, o.Customer.Email),
                o.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new OrderItemResponse(
                        i.Id,
                        i.ProductId,
                        i.Product.Name,
                        i.Quantity,
                        i.UnitPrice
                        ))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return order ?? throw new NotFoundException($"Order with id {id} was not found.");
    }

    public async Task<OrderResponse> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        await _statusValidator.ValidateAndThrowAsync(request, ct);

        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException($"Order with id {id} was not found.");

        if (order.Status == request.Status)
            throw new ConflictException($"Order is already {order.Status}.");

        if (!order.CanChangeStatusTo(request.Status))
            throw new ConflictException($"Cannot change order status from {order.Status} to {request.Status}.");

        if (request.Status == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
                item.Product.StockQuantity += item.Quantity;
        }
        order.Status = request.Status;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(order.Id, ct);
    }
}