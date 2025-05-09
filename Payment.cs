using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    public class Payment
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public int PaymentId { get; set; } 

        [Required]
        public int OrderId { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public double Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        public bool IsRefunded { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } 
        public Payment() { } 

        public Payment(int paymentId, int orderId, double amount, string paymentMethod = null)
        {
            if (paymentId <= 0)
                throw new ArgumentException("Payment ID must be positive", nameof(paymentId));

            if (orderId <= 0)
                throw new ArgumentException("Order ID must be positive", nameof(orderId));

            if (amount <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amount));

            PaymentId = paymentId;
            OrderId = orderId;
            Amount = amount;
            PaymentMethod = paymentMethod ?? ChoosePaymentMethod();
            PaymentDate = DateTime.Now;
            IsRefunded = false;
        }

        public void ProcessPayment(ECommerceDbContext context)
        {
            try
            {
                if (Amount <= 0)
                    throw new InvalidOperationException("Cannot process zero or negative payment");

                if (PaymentId <= 0)
                {
                    PaymentId = GeneratePaymentId(context);
                }

                PaymentDate = DateTime.Now;
                Console.WriteLine($"Processing ${Amount:F2} payment via {PaymentMethod}...");
               
                System.Threading.Thread.Sleep(1500);

                if (Id == 0) 
                {
                    context.Payments.Add(this);
                }
                else 
                {
                    context.Payments.Update(this);
                }

                context.SaveChanges();
                Console.WriteLine($"\nPayment #{PaymentId} completed successfully!");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError processing payment: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error: {ex.Message}");
                throw;
            }
        }

        public void RefundPayment(ECommerceDbContext context)
        {
            try
            {
                if (IsRefunded)
                    throw new InvalidOperationException("Payment has already been refunded");

                if (Amount <= 0)
                    throw new InvalidOperationException("Cannot refund zero or negative amount");

                Console.WriteLine($"Initiating refund of ${Amount:F2} for Payment #{PaymentId}...");
                
                System.Threading.Thread.Sleep(1500);
                IsRefunded = true;

                context.Payments.Update(this);
                context.SaveChanges();
                Console.WriteLine($"Refund of ${Amount:F2} completed successfully!");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error refunding payment: {ex.InnerException?.Message ?? ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }

        public static string ChoosePaymentMethod()
        {
            var methods = new Dictionary<int, string>
            {
                {1, "Credit Card"},
                {2, "PayPal"},
                {3, "Bank Transfer"},
                {4, "Cash on Delivery"}
            };

            while (true)
            {
                Console.WriteLine("\nAvailable Payment Methods:");
                foreach (var method in methods)
                {
                    Console.WriteLine($"{method.Key}. {method.Value}");
                }

                Console.Write("Select payment method (1-4): ");
                if (int.TryParse(Console.ReadLine(), out int choice) && methods.ContainsKey(choice))
                {
                    return methods[choice];
                }
                Console.WriteLine("Invalid selection. Please try again.");
            }
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
    }
}