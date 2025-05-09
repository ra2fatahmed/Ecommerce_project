using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    public class ECommerceDbContext : DbContext
    {
        // DbSet properties for all entities
        public DbSet<User> Users { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Admin> Admins { get; set; }

       
        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User configuration
            modelBuilder.Entity<User>(user =>
            {
                user.HasKey(u => u.Id);
                user.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(255);
                user.HasIndex(u => u.Email)
                    .IsUnique();
                user.Property(u => u.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                user.Property(u => u.Password)
                    .IsRequired()
                    .HasMaxLength(100);
                user.Property(u => u.PhoneNumber)
                    .HasMaxLength(20);
                user.HasOne(u => u.UserAddress)
                    .WithMany()
                    .HasForeignKey(u => u.UserAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Address configuration
            modelBuilder.Entity<Address>(address =>
            {
                address.HasKey(a => a.Id);
                address.Property(a => a.StreetNum)
                    .IsRequired()
                    .HasMaxLength(50);
                address.Property(a => a.Street)
                    .IsRequired()
                    .HasMaxLength(100);
                address.Property(a => a.City)
                    .IsRequired()
                    .HasMaxLength(100);
                address.Property(a => a.State)
                    .IsRequired()
                    .HasMaxLength(100);
                address.Property(a => a.Country)
                    .IsRequired()
                    .HasMaxLength(100);
                address.Property(a => a.ZipCode)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            // Product configuration
            modelBuilder.Entity<Product>(product =>
            {
                product.HasKey(p => p.Id);
                product.Property(p => p.prod_id)
                    .IsRequired();
                product.HasIndex(p => p.prod_id)
                    .IsUnique();
                product.Property(p => p.prod_name)
                    .IsRequired()
                    .HasMaxLength(100);
                product.Property(p => p.set_prod_description)
                    .HasMaxLength(500);
                product.Property(p => p.prod_price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                product.Property(p => p.prod_stock)
                    .IsRequired();
            });

            // Cart configuration
            modelBuilder.Entity<Cart>(cart =>
            {
                cart.HasKey(c => c.Id);
                cart.Property(c => c.CreatedDate)
                    .IsRequired();
                cart.Property(c => c.LastUpdatedDate);
                cart.Property(c => c.CustomerId);
                cart.HasMany(c => c.Items)
                    .WithOne(ci => ci.Cart)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);
                cart.HasOne(c => c.Customer)
                    .WithOne(cu => cu.ShoppingCart)
                    .HasForeignKey<Cart>(c => c.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // CartItem configuration
            modelBuilder.Entity<CartItem>(cartItem =>
            {
                cartItem.HasKey(ci => ci.Id);
                cartItem.Property(ci => ci.CartId)
                    .IsRequired();
                cartItem.Property(ci => ci.ProductId)
                    .IsRequired();
                cartItem.Property(ci => ci.ProductName)
                    .IsRequired()
                    .HasMaxLength(100);
                cartItem.Property(ci => ci.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                cartItem.Property(ci => ci.Quantity)
                    .IsRequired();
                cartItem.HasOne(ci => ci.Cart)
                    .WithMany(c => c.Items)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);
                cartItem.HasOne(ci => ci.Product)
                    .WithMany()
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Order configuration
            modelBuilder.Entity<Order>(order =>
            {
                order.HasKey(o => o.Id);
                order.Property(o => o.OrderId)
                    .IsRequired();
                order.HasIndex(o => o.OrderId)
                    .IsUnique();
                order.Property(o => o.OrderDate)
                    .IsRequired();
                order.Property(o => o.TotalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                order.Property(o => o.Status)
                    .IsRequired()
                    .HasMaxLength(50);
                order.Property(o => o.CustomerId)
                    .IsRequired();
                order.Property(o => o.ShippingAddressId)
                    .IsRequired();
                order.Property(o => o.TrackingNumber)
                    .HasMaxLength(100);
                order.HasOne(o => o.Customer)
                    .WithMany(c => c.OrderHistory)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                order.HasOne(o => o.ShippingAddress)
                    .WithMany()
                    .HasForeignKey(o => o.ShippingAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
                order.HasMany(o => o.Items)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                order.HasOne(o => o.Payment)
                    .WithOne(p => p.Order)
                    .HasForeignKey<Payment>(p => p.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OrderItem configuration
            modelBuilder.Entity<OrderItem>(orderItem =>
            {
                orderItem.HasKey(oi => oi.Id);
                orderItem.Property(oi => oi.OrderId)
                    .IsRequired();
                orderItem.Property(oi => oi.ProductId)
                    .IsRequired();
                orderItem.Property(oi => oi.ProductName)
                    .IsRequired()
                    .HasMaxLength(100);
                orderItem.Property(oi => oi.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                orderItem.Property(oi => oi.Quantity)
                    .IsRequired();
                orderItem.HasOne(oi => oi.Order)
                    .WithMany(o => o.Items)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                orderItem.HasOne(oi => oi.Product)
                    .WithMany()
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment configuration
            modelBuilder.Entity<Payment>(payment =>
            {
                payment.HasKey(p => p.Id);
                payment.Property(p => p.PaymentId)
                    .IsRequired();
                payment.HasIndex(p => p.PaymentId)
                    .IsUnique();
                payment.Property(p => p.OrderId)
                    .IsRequired();
                payment.Property(p => p.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                payment.Property(p => p.PaymentDate)
                    .IsRequired();
                payment.Property(p => p.PaymentMethod)
                    .IsRequired()
                    .HasMaxLength(50);
                payment.Property(p => p.IsRefunded)
                    .IsRequired();
                payment.HasOne(p => p.Order)
                    .WithOne(o => o.Payment)
                    .HasForeignKey<Payment>(p => p.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Customer configuration
            modelBuilder.Entity<Customer>(customer =>
            {
                customer.HasOne(c => c.ShoppingCart)
                        .WithOne(cu => cu.Customer)
                        .HasForeignKey<Cart>(c => c.CustomerId)
                        .OnDelete(DeleteBehavior.Restrict);
                customer.HasMany(c => c.OrderHistory)
                        .WithOne(o => o.Customer)
                        .HasForeignKey(o => o.CustomerId)
                        .OnDelete(DeleteBehavior.Restrict);
            });

            // Inheritance configuration for User, Customer, Admin
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<User>("User")
                .HasValue<Customer>("Customer")
                .HasValue<Admin>("Admin");

            modelBuilder.Entity<Customer>()
                .HasBaseType<User>();

            modelBuilder.Entity<Admin>()
                .HasBaseType<User>();
        }
    }
}