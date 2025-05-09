using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    public class Cart
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public DateTime CreatedDate { get; set; }

        public DateTime? LastUpdatedDate { get; set; }

        public List<CartItem> Items { get; set; }

        public int? CustomerId { get; set; } 

        public Customer Customer { get; set; } 

        public Cart()
        {
            CreatedDate = DateTime.Now;
            Items = new List<CartItem>();
        }

        public void AddItem(ECommerceDbContext context, CartItem item)
        {
            try
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                var existingItem = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity += item.Quantity;
                    context.CartItems.Update(existingItem);
                }
                else
                {
                    item.CartId = this.Id;
                    Items.Add(item);
                    context.CartItems.Add(item);
                }

                LastUpdatedDate = DateTime.Now;
                context.Carts.Update(this);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError adding item: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public bool RemoveItem(ECommerceDbContext context, int productId)
        {
            try
            {
                var itemToRemove = Items.FirstOrDefault(item => item.ProductId == productId);
                if (itemToRemove == null)
                {
                    return false;
                }

                Items.Remove(itemToRemove);
                context.CartItems.Remove(itemToRemove);
                LastUpdatedDate = DateTime.Now;
                context.Carts.Update(this);
                context.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError removing item from cart: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error: {ex.Message}");
                return false;
            }
        }

        public bool UpdateItemQuantity(ECommerceDbContext context, int productId, int newQuantity)
        {
            try
            {
                var item = Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    item.UpdateQuantity(context, newQuantity);
                    LastUpdatedDate = DateTime.Now;
                    context.Carts.Update(this);
                    context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError updating item quantity: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error: {ex.Message}");
                return false;
            }
        }

        public double CalculateTotal()
        {
            return Items.Sum(item => item.Price * item.Quantity);
        }

        public double ApplyCoupon(ECommerceDbContext context, string couponCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(couponCode))
                    return CalculateTotal();

                double discountPercentage = couponCode.ToUpper() switch
                {
                    "SAVE10" => 0.10,
                    "SAVE20" => 0.20,
                    "SAVE30" => 0.30,
                    "SAVE40" => 0.40,
                    _ => 0.0
                };

                double discount = CalculateTotal() * discountPercentage;
                double totalAfterDiscount = CalculateTotal() - discount;

                LastUpdatedDate = DateTime.Now;
                context.Carts.Update(this);
                context.SaveChanges();

                return totalAfterDiscount > 0 ? totalAfterDiscount : 0;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error applying coupon: {ex.InnerException?.Message ?? ex.Message}");
                return CalculateTotal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return CalculateTotal();
            }
        }

        public void ClearCart(ECommerceDbContext context)
        {
            try
            {
                context.CartItems.RemoveRange(Items);
                Items.Clear();
                LastUpdatedDate = DateTime.Now;
                context.Carts.Update(this);
                context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError clearing cart: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error: {ex.Message}");
            }
        }

        public void DisplayCart()
        {
            if (Items.Count == 0)
            {
                Console.WriteLine("\nYour cart is empty.");
                return;
            }

            Console.WriteLine("\n=== YOUR CART ===");
            foreach (var item in Items)
            {
                Console.WriteLine($"ID: {item.ProductId}");
                Console.WriteLine($"Name: {item.ProductName}");
                Console.WriteLine($"Price: {item.Price:C}");
                Console.WriteLine($"Quantity: {item.Quantity}");
                Console.WriteLine($"Subtotal: {item.Price * item.Quantity:C}");
                Console.WriteLine("-------------------");
            }

            Console.WriteLine($"\nTOTAL: {CalculateTotal():C}");
        }
    }
}