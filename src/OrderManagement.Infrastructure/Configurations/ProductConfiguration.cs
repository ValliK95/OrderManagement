using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderManagement.Infrastructure.Configurations
{
    public class ProductConfiguration
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Price).HasPrecision(18, 2);

            builder.HasIndex(p => p.Sku).IsUnique();
            builder.HasIndex(p => p.Name);
        }
    }
}
