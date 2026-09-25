using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderManagement.Tests
{
    public class OrderStatusTests
    {
        [Theory]
        [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
        [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
        public void CanChangeStatusTo_AllowedChange_ReturnsTrue(OrderStatus current, OrderStatus next)
        {
            var order = new Order { Status = current };

            Assert.True(order.CanChangeStatusTo(next));
        }

        [Theory]
        [InlineData(OrderStatus.Delivered, OrderStatus.Pending)]
        [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]
        public void CanChangeStatusTo_NotAllowedChange_ReturnsFalse(OrderStatus current, OrderStatus next)
        {
            var order = new Order { Status = current };

            Assert.False(order.CanChangeStatusTo(next));
        }
    }
}
