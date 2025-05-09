using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public int CartId { get; set; } 

        [Required]
        public int ProductId { get; set; } 

        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public double Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        [ForeignKey("CartId")]
        public Cart Cart { get; set; } 

        [ForeignKey("ProductId")]
        public Product Product { get; set; } 

        public double TotalPrice => Price * Quantity;

        public CartItem() { } 

        public CartItem(int productId, string productName, double price, int quantity)
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be positive", nameof(productId));

            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name cannot be empty", nameof(productName));

            if (price <= 0)
                throw new ArgumentException("Price must be positive", nameof(price));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            ProductId = productId;
            ProductName = productName;
            Price = price;
            Quantity = quantity;
        }

        public void UpdateQuantity(ECommerceDbContext context, int newQuantity)
        {
            try
            {
                if (newQuantity <= 0)
                    throw new ArgumentException("Quantity must be positive", nameof(newQuantity));

                var product = context.Products.FirstOrDefault(p => p.prod_id == ProductId);
                if (product == null)
                    throw new InvalidOperationException($"Product with ID {ProductId} not found.");

                product.prod_stock += Quantity; 
                if (product.prod_stock < newQuantity)
                    throw new InvalidOperationException($"Only {product.prod_stock} items available.");

                product.prod_stock -= newQuantity; 
                Quantity = newQuantity;

                context.Products.Update(product);
                context.CartItems.Update(this);
                context.SaveChanges();

                Console.WriteLine($"\nQuantity updated to {newQuantity} for {ProductName}.");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError updating quantity: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"{ProductName} (ID: {ProductId}) - {Quantity} x {Price:C} = {TotalPrice:C}";
        }
    }
}