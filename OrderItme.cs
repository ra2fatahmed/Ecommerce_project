using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace e_commerce
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public int OrderId { get; set; } 

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

        [ForeignKey("OrderId")]
        public Order Order { get; set; } 

        [ForeignKey("ProductId")]
        public Product Product { get; set; } 

        public OrderItem() { } 

        public OrderItem(int productId, string productName, double price, int quantity)
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be positive", nameof(productId));

            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name cannot be empty", nameof(productName));

            if (price <= 0)
                throw new ArgumentException("Price must be positive", nameof(price));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

            ProductId = productId;
            ProductName = productName;
            Price = price;
            Quantity = quantity;
        }

        public double GetTotalPrice()
        {
            return Price * Quantity;
        }

        public override string ToString()
        {
            return $"{ProductName} (ID: {ProductId}) - {Quantity} x ${Price:F2} = ${GetTotalPrice():F2}";
        }
    }
}