using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    public class Order
    {
        public const string STATUS_PLACED = "Placed";
        public const string STATUS_PROCESSING = "Processing";
        public const string STATUS_SHIPPED = "Shipped";
        public const string STATUS_DELIVERED = "Delivered";
        public const string STATUS_CANCELLED = "Cancelled";
        public const string STATUS_REFUNDED = "Refunded";

        [Key]
        public int Id { get; set; } 

        [Required]
        public int OrderId { get; set; } 

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public double TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        [Required]
        public int CustomerId { get; set; } 

        [Required]
        public int ShippingAddressId { get; set; } 

        [MaxLength(100)]
        public string TrackingNumber { get; set; }

        public DateTime? ShipmentDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public List<OrderItem> Items { get; set; }

        public Payment Payment { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } 

        [ForeignKey("ShippingAddressId")]
        public Address ShippingAddress { get; set; } 

        public Order() 
        {
            Items = new List<OrderItem>();
            OrderDate = DateTime.Now;
            Status = STATUS_PLACED;
            TrackingNumber = string.Empty;
        }

        public Order(int orderId, Address shippingAddress)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be positive", nameof(orderId));

            if (shippingAddress == null)
                throw new ArgumentNullException(nameof(shippingAddress));

            OrderId = orderId;
            OrderDate = DateTime.Now;
            Status = STATUS_PLACED;
            Items = new List<OrderItem>();
            ShippingAddress = shippingAddress;
            TrackingNumber = string.Empty;
        }

        #region Order Lifecycle Methods
        public void ProcessPayment(ECommerceDbContext context, string paymentMethod)
        {
            try
            {
                ValidateOrderStatus(STATUS_PLACED, "process payment for");

                if (TotalAmount <= 0)
                    throw new InvalidOperationException("Cannot process payment for zero amount");

                int paymentId = GeneratePaymentId(context);
                if (paymentId <= 0)
                    throw new InvalidOperationException("Generated payment ID is invalid");

                Payment = new Payment(paymentId, OrderId, TotalAmount, paymentMethod);
                Payment.ProcessPayment(context);
                Status = STATUS_PROCESSING;

                context.Orders.Update(this);
                context.SaveChanges();

                Console.WriteLine($"Payment processed successfully for Order #{OrderId}");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error processing payment: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }

        public void ShipOrder(ECommerceDbContext context, string trackingNumber)
        {
            try
            {
                ValidateOrderStatus(STATUS_PROCESSING, "ship");

                if (string.IsNullOrWhiteSpace(trackingNumber))
                    throw new ArgumentException("Tracking number cannot be empty", nameof(trackingNumber));

                Status = STATUS_SHIPPED;
                TrackingNumber = trackingNumber;
                ShipmentDate = DateTime.Now;

                context.Orders.Update(this);
                context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error shipping order: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public void MarkAsDelivered(ECommerceDbContext context)
        {
            try
            {
                
                if (Status != STATUS_SHIPPED)
                {
                    throw new InvalidOperationException(
                        $"Order must be in {STATUS_SHIPPED} status to be marked as delivered. Current status: {Status}");
                }

                Status = STATUS_DELIVERED;
                DeliveryDate = DateTime.Now;

                
                if (Customer is Customer customer)
                {
                    int pointsEarned = (int)(TotalAmount / 10); 
                    customer.LoyaltyPoints += pointsEarned;
                }

                context.Orders.Update(this);
                context.SaveChanges();

                
                SendDeliveryNotification();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error marking order as delivered: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }

        private void SendDeliveryNotification()
        {
            
            Console.WriteLine($"\nDelivery notification for order #{OrderId}");
            Console.WriteLine($"Your order has been delivered on {DeliveryDate:g}");
            Console.WriteLine($"Thank you for shopping with us!");
        }

        public void CancelOrder(ECommerceDbContext context)
        {
            try
            {
                if (Status == STATUS_SHIPPED || Status == STATUS_DELIVERED)
                    throw new InvalidOperationException($"Cannot cancel order that has been {Status.ToLower()}");

                Status = STATUS_CANCELLED;

                if (Payment != null && !Payment.IsRefunded)
                {
                    Payment.RefundPayment(context);
                    Status = STATUS_REFUNDED;
                }

                context.Orders.Update(this);
                context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error cancelling order: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
        #endregion

        #region Item Management
        public void AddItem(ECommerceDbContext context, OrderItem item)
        {
            try
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                ValidateOrderStatus(STATUS_PLACED, "add items to");

                var existingItem = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity += item.Quantity;
                    context.OrderItems.Update(existingItem);
                }
                else
                {
                    item.OrderId = OrderId;
                    Items.Add(item);
                    context.OrderItems.Add(item);
                }

                CalculateTotal();
                context.Orders.Update(this);
                context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error adding item to order: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public bool RemoveItem(ECommerceDbContext context, int productId)
        {
            try
            {
                ValidateOrderStatus(STATUS_PLACED, "remove items from");

                var itemToRemove = Items.FirstOrDefault(i => i.ProductId == productId);
                if (itemToRemove != null)
                {
                    Items.Remove(itemToRemove);
                    context.OrderItems.Remove(itemToRemove);
                    CalculateTotal();
                    context.Orders.Update(this);
                    context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error removing item from order: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return false;
            }
        }

        public void UpdateItemQuantity(ECommerceDbContext context, int productId, int newQuantity)
        {
            try
            {
                ValidateOrderStatus(STATUS_PLACED, "modify items in");

                var item = Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    if (newQuantity <= 0)
                        throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

                    item.Quantity = newQuantity;
                    context.OrderItems.Update(item);
                    CalculateTotal();
                    context.Orders.Update(this);
                    context.SaveChanges();
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error updating item quantity: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods
        private void CalculateTotal()
        {
            TotalAmount = Items.Sum(item => item.Price * item.Quantity);
        }

        private int GeneratePaymentId(ECommerceDbContext context)
        {
            int paymentId;
            int maxAttempts = 10;
            int attempt = 0;

            do
            {
                if (attempt++ >= maxAttempts)
                    throw new InvalidOperationException("Unable to generate a unique payment ID after multiple attempts");

                paymentId = new Random().Next(100000, 999999);
            } while (context.Payments.Any(p => p.PaymentId == paymentId));

            return paymentId;
        }

        private void ValidateOrderStatus(string expectedStatus, string action)
        {
            if (Status != expectedStatus)
                throw new InvalidOperationException($"Cannot {action} order in current status: {Status}");
        }
        #endregion

        #region Display Methods
        public void DisplaySummary()
        {
            Console.WriteLine($"Order #{OrderId} - {Status}");
            Console.WriteLine($"Date: {OrderDate:yyyy-MM-dd}");
            Console.WriteLine($"Total: ${TotalAmount:F2}");
            Console.WriteLine($"Items: {Items.Count}");

            if (!string.IsNullOrEmpty(TrackingNumber))
                Console.WriteLine($"Tracking: {TrackingNumber}");
        }

        public void DisplayFullDetails()
        {
            Console.WriteLine($"\n=== ORDER #{OrderId} ===");
            Console.WriteLine($"Status: {Status}");
            Console.WriteLine($"Date: {OrderDate:yyyy-MM-dd HH:mm}");

            if (ShipmentDate.HasValue)
                Console.WriteLine($"Shipped: {ShipmentDate.Value:yyyy-MM-dd}");
            if (DeliveryDate.HasValue)
                Console.WriteLine($"Delivered: {DeliveryDate.Value:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(TrackingNumber))
                Console.WriteLine($"Tracking: {TrackingNumber}");

            Console.WriteLine("\nItems:");
            foreach (var item in Items)
            {
                Console.WriteLine($"- {item.ProductName} (ID: {item.ProductId})");
                Console.WriteLine($"  {item.Quantity} x ${item.Price:F2} = ${item.Quantity * item.Price:F2}");
            }

            Console.WriteLine($"\nTOTAL: ${TotalAmount:F2}");
            Console.WriteLine($"Shipping to: {ShippingAddress}");

            if (Payment != null)
            {
                Console.WriteLine($"\nPayment: {Payment.PaymentMethod}");
                Console.WriteLine($"Paid: ${Payment.Amount:F2} on {Payment.PaymentDate:yyyy-MM-dd}");
                if (Payment.IsRefunded)
                    Console.WriteLine("(Payment has been refunded)");
            }

            Console.WriteLine("=====================");
        }
        #endregion
    }
}