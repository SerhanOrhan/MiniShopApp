using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniShopApp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniShopApp.Data.Config
{
    public class ProductConfig : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.ProductId);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);

            //Sqlite için bu date 
            builder.Property(p => p.DateAdded).HasDefaultValueSql("date('now')");

            //Sql server için  oto date
            //builder.Property(p => p.DateAdded).HasDefaultValueSql("getdate()");
        }
    }
}
