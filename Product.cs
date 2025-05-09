using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int prod_id { get; set; } 

        [Required]
        [MaxLength(100)]
        public string prod_name { get; set; }

        [MaxLength(500)]
        public string set_prod_description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public double prod_price { get; set; }

        [Required]
        public int prod_stock { get; set; }

        public Product() { } 

        public Product(int productId, string productName, string productDescription, double price, int stockQuantity)
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be positive", nameof(productId));

            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name cannot be empty", nameof(productName));

            if (price <= 0)
                throw new ArgumentException("Price must be positive", nameof(price));

            if (stockQuantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

            prod_id = productId;
            prod_name = productName;
            set_prod_description = productDescription;
            prod_price = price;
            prod_stock = stockQuantity;
        }

        public void display_product()
        {
            Console.WriteLine("\n--- Product Details ---");
            Console.WriteLine($"Product Name: {prod_name}");
            Console.WriteLine($"Product ID: {prod_id}");
            Console.WriteLine($"Product Price: {prod_price:C}");
            Console.WriteLine($"Product Description: {set_prod_description}");
            Console.WriteLine($"Stock: {prod_stock}");
            Console.WriteLine("-----------------------");
        }

        public bool UpdateStock(ECommerceDbContext context, int quantity)
        {
            try
            {
                if (quantity == 0)
                {
                    Console.WriteLine("Please enter a quantity greater than 0.");
                    return false;
                }

                if (quantity > 0)
                {
                    prod_stock += quantity;
                    Console.WriteLine($"{quantity} units added to stock.");
                }

                else
                {
                    int newStock = prod_stock + quantity;

                    if (newStock >= 0)
                    {
                        prod_stock = newStock;
                        Console.WriteLine($"{Math.Abs(quantity)} units removed from stock.");
                    }
                    else
                    {
                        Console.WriteLine("Not enough stock to remove the requested quantity.");
                        return false;
                    }
                }

                if (prod_stock == 0)
                {
                    context.Products.Remove(this);
                    Console.WriteLine($"Product '{prod_name}' removed due to zero stock.");
                }
                else
                {
                    context.Products.Update(this);
                }

                context.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error updating stock: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return false;
            }
        }

    }
}