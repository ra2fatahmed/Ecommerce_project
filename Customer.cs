using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    
    public class Customer : User
    {
        public List<Order> OrderHistory { get; set; } = new List<Order>();
        public Cart ShoppingCart { get; set; }
        public int LoyaltyPoints { get; set; } = 0;

        protected Customer() 
        {
            Role = "Customer";
            IsLogin = false;
        } 

        public Customer(int id, string name, string email, string password, int age, string phoneNumber, Address address)
            : base(id, name, email, password, age, phoneNumber, address)
        {
            Role = "Customer";
            LoyaltyPoints = 0;
            IsLogin = false;
        }

        public static new Customer Register(ECommerceDbContext context)
        {
            try
            {
                Console.Write("Enter your name: ");
                string name = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("Name cannot be empty.");
                    return null;
                }

                string email;
                while (true)
                {
                    Console.Write("Enter your email (@example.com): ");
                    email = Console.ReadLine()?.Trim() ?? "";
                    if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        Console.WriteLine("Invalid email format! Try again.");
                        continue;
                    }
                    if (context.Users.Any(u => u.Email == email.ToLower()))
                    {
                        Console.WriteLine("Email already registered. Please use a different email.");
                        continue;
                    }
                    break;
                }

                string password;
                while (true)
                {
                    Console.Write("Enter your password (6-10 characters): ");
                    password = Console.ReadLine()?.Trim() ?? "";
                    if (password.Length >= 6 && password.Length <= 10)
                        break;
                    Console.WriteLine("Password must be between 6 and 10 characters! Try again.");
                }

                int age;
                while (true)
                {
                    Console.Write("Enter your age: ");
                    if (int.TryParse(Console.ReadLine(), out age))
                        break;
                    Console.WriteLine("Invalid age. Please enter a number.");
                }

                Console.Write("Enter your phone number: ");
                string phoneNumber = Console.ReadLine()?.Trim() ?? "";

                Console.WriteLine("\n== Address details ==");
                
                var address = new Address
                {
                    StreetNum = GetInput("street number", true),
                    Street = GetInput("street", true),
                    City = GetInput("city", true),
                    State = GetInput("state", true),
                    Country = GetInput("country", true),
                    ZipCode = GetInput("ZipCode", true)
                    
                };

                var customer = new Customer(0, name, email, password, age, phoneNumber, address);
                var cart = new Cart { Customer = customer };

                context.Users.Add(customer);
                context.SaveChanges();

                Console.WriteLine("Customer registered successfully!");
                return customer;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error registering customer: {ex.InnerException?.Message ?? ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return null;
            }
        }

        private static string GetInput(string fieldName, bool required)
        {
            while (true)
            {
                Console.Write($"Enter your {fieldName}: ");
                string input = Console.ReadLine()?.Trim() ?? "";
                if (!required || !string.IsNullOrWhiteSpace(input))
                    return input;
                Console.WriteLine($"{fieldName} cannot be empty.");
            }
        }

        public void AddToCart(ECommerceDbContext context, int productIdInput, int quantity)
        {
            try
            {
                if (quantity <= 0)
                {
                    Console.WriteLine("Quantity must be positive.");
                    return;
                }

                var product = context.Products.FirstOrDefault(p => p.prod_id == productIdInput);
                if (product == null)
                {
                    Console.WriteLine("Product not found!");
                    return;
                }

                if (product.prod_stock < quantity)
                {
                    Console.WriteLine($"Not enough stock available for '{product.prod_name}'. Available: {product.prod_stock}, Requested: {quantity}.");
                    if (product.prod_stock > 0)
                    {
                        Console.Write($"Would you like to add {product.prod_stock} units instead? (Y/N): ");
                        if (Console.ReadLine().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                        {
                            quantity = product.prod_stock;
                        }
                        else
                        {
                            Console.WriteLine("Operation cancelled.");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                if (ShoppingCart == null)
                {
                    Console.WriteLine("Creating new cart...");
                    ShoppingCart = new Cart
                    {
                        CustomerId = Id,
                        CreatedDate = DateTime.Now,
                        LastUpdatedDate = DateTime.Now,
                        Items = new List<CartItem>()
                    };
                    context.Carts.Add(ShoppingCart);
                    context.SaveChanges();
                }

                var cart = context.Carts
                    .Include(c => c.Items)
                    .FirstOrDefault(c => c.Id == ShoppingCart.Id);

                if (cart == null)
                {
                    Console.WriteLine("Error retrieving cart after creation!");
                    return;
                }

                using var transaction = context.Database.BeginTransaction();
                try
                {
                    var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == product.Id);
                    if (existingItem != null)
                    {
                        // التحقق من توفر الكمية الإضافية
                        if (product.prod_stock < existingItem.Quantity + quantity)
                        {
                            Console.WriteLine($"Not enough stock to add {quantity} more of '{product.prod_name}'. Available: {product.prod_stock}.");
                            if (product.prod_stock > existingItem.Quantity)
                            {
                                Console.Write($"Would you like to add {product.prod_stock - existingItem.Quantity} units instead? (Y/N): ");
                                if (Console.ReadLine().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                                {
                                    quantity = product.prod_stock - existingItem.Quantity;
                                }
                                else
                                {
                                    Console.WriteLine("Operation cancelled.");
                                    transaction.Rollback();
                                    return;
                                }
                            }
                            else
                            {
                                transaction.Rollback();
                                return;
                            }
                        }
                        existingItem.Quantity += quantity;
                        context.CartItems.Update(existingItem);
                    }
                    else
                    {
                        var newItem = new CartItem(product.Id, product.prod_name, product.prod_price, quantity)
                        {
                            CartId = cart.Id
                        };
                        context.CartItems.Add(newItem);
                        cart.Items.Add(newItem);
                    }

                    cart.LastUpdatedDate = DateTime.Now;
                    context.Carts.Update(cart);

                    context.SaveChanges();
                    transaction.Commit();

                    ShoppingCart = cart;
                    Console.WriteLine($"Added {quantity} of '{product.prod_name}' to cart successfully!");
                    Console.WriteLine($"Available stock for '{product.prod_name}': {product.prod_stock}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Error adding to cart: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System error: {ex.Message}");
            }
        }

        public void RemoveFromCart(ECommerceDbContext context, int productId)
        {
            try
            {
                var cart = context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefault(c => c.Id == ShoppingCart.Id);

                if (cart == null || cart.Items == null || !cart.Items.Any())
                {
                    Console.WriteLine("Your cart is empty.");
                    return;
                }

                var cartItem = cart.Items.FirstOrDefault(i => i.Product != null && i.Product.prod_id == productId);
                if (cartItem == null)
                {
                    Console.WriteLine($"Product with ID {productId} not found in your cart.");
                    return;
                }

                using var transaction = context.Database.BeginTransaction();
                try
                {
                    cart.Items.Remove(cartItem);
                    context.CartItems.Remove(cartItem);

                    cart.LastUpdatedDate = DateTime.Now;
                    context.Carts.Update(cart);

                    context.SaveChanges();
                    transaction.Commit();

                    Console.WriteLine($"'{cartItem.ProductName}' removed from cart successfully.");
                    Console.WriteLine($"Cart updated. Total items: {cart.Items.Count}");
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Database error: {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System error: {ex.Message}");
            }
        }

        public void ViewCart(ECommerceDbContext context)
        {
            var cart = context.Carts
                .Include(c => c.Items)
                .FirstOrDefault(c => c.CustomerId == this.Id);

            if (cart == null)
            {
                Console.WriteLine("No cart found. Creating a new cart for you...");
                cart = new Cart
                {
                    CustomerId = this.Id,
                    CreatedDate = DateTime.Now,
                    LastUpdatedDate = DateTime.Now,
                    Items = new List<CartItem>()
                };
                context.Carts.Add(cart);
                context.SaveChanges();
            }

            this.ShoppingCart = cart;

            if (cart.Items == null || cart.Items.Count == 0)
            {
                Console.WriteLine("Your cart is empty.");
                return;
            }

            cart.DisplayCart();

            if (LoyaltyPoints > 0)
            {
                Console.WriteLine($"\nYou have {LoyaltyPoints} loyalty points available");
                Console.Write("Would you like to redeem points? (Y/N): ");
                if (Console.ReadLine().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    RedeemLoyaltyPoints(context);
                }
            }
        }

        private void RedeemLoyaltyPoints(ECommerceDbContext context)
        {
            try
            {
                Console.Write($"Enter points to redeem (1-{LoyaltyPoints}): ");
                if (int.TryParse(Console.ReadLine(), out int points) && points > 0 && points <= LoyaltyPoints)
                {
                    double discount = points;
                    double total = ShoppingCart.CalculateTotal();
                    double newTotal = total - discount;

                    Console.WriteLine($"Applied {points} points (${discount:F2} discount)");
                    Console.WriteLine($"New Total: {newTotal:F2}");
                    LoyaltyPoints -= points;

                    context.Users.Update(this);
                    context.SaveChanges();
                }
                else
                {
                    Console.WriteLine("Invalid points entered. No changes made.");
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error redeeming points: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public void Checkout(string paymentMethod = null)
        {
            try
            {
                if (ShoppingCart == null)
                {
                    Console.WriteLine("No shopping cart found. Please create a cart first.");
                    return;
                }

                if (!IsLogin)
                {
                    Console.WriteLine("Please login before checkout.");
                    return;
                }

                DbContextOptions<ECommerceDbContext> options = new DbContextOptionsBuilder<ECommerceDbContext>()
                              .UseSqlServer("Server=DESKTOP-2LBBLI4\\SQLEXPRESS;Database=ECommerce_new2;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30;")
                              .Options;

                using var context = new ECommerceDbContext(options);

                var cart = context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefault(c => c.Id == ShoppingCart.Id);

                if (cart == null || !cart.Items.Any())
                {
                    Console.WriteLine("Your cart is empty.");
                    return;
                }

                if (UserAddress == null)
                {
                    Console.WriteLine("No shipping address provided. Would you like to add one now? (Y/N): ");
                    if (Console.ReadLine().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        UserAddress = new Address();
                        UserAddress.UpdateAddress(context);
                    }
                    else
                    {
                        Console.WriteLine("Checkout cancelled. Please provide a shipping address.");
                        return;
                    }
                }

                using var transaction = context.Database.BeginTransaction();
                try
                {
                    var unavailableItems = new List<(string Name, int Available, int Requested)>();
                    var removedItems = new List<string>();

                    foreach (var item in cart.Items.ToList())
                    {
                        var product = item.Product ?? context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product == null || product.prod_stock <= 0)
                        {
                            removedItems.Add(item.ProductName);
                            cart.Items.Remove(item);
                            context.CartItems.Remove(item);
                            continue;
                        }

                        if (product.prod_stock < item.Quantity)
                        {
                            unavailableItems.Add((product.prod_name, product.prod_stock, item.Quantity));
                        }
                    }

                    if (removedItems.Any())
                    {
                        Console.WriteLine("\nThe following items were removed as they're no longer available:");
                        removedItems.ForEach(item => Console.WriteLine($"- {item}"));
                    }

                    if (unavailableItems.Any())
                    {
                        Console.WriteLine("\nStock issues detected:");
                        foreach (var item in unavailableItems)
                        {
                            Console.WriteLine($"- {item.Name} (Available: {item.Available}, Requested: {item.Requested})");
                        }

                        Console.WriteLine("\nOptions:");
                        Console.WriteLine("1. Adjust quantities to available stock");
                        Console.WriteLine("2. Remove items with insufficient stock");
                        Console.WriteLine("3. Cancel checkout");
                        Console.Write("Select an option (1-3): ");
                        string choice = Console.ReadLine()?.Trim();

                        if (choice == "1")
                        {
                            foreach (var item in unavailableItems)
                            {
                                var cartItem = cart.Items.First(i => i.Product.prod_name == item.Name);
                                if (item.Available > 0)
                                {
                                    cartItem.Quantity = item.Available;
                                    context.CartItems.Update(cartItem);
                                    Console.WriteLine($"Adjusted quantity for '{item.Name}' to {item.Available}.");
                                }
                                else
                                {
                                    cart.Items.Remove(cartItem);
                                    context.CartItems.Remove(cartItem);
                                    Console.WriteLine($"Removed '{item.Name}' due to zero stock.");
                                }
                            }
                            context.SaveChanges();
                            Console.WriteLine("Cart updated. Please try checkout again.");
                            transaction.Rollback();
                            return;
                        }
                        else if (choice == "2")
                        {
                            foreach (var item in unavailableItems)
                            {
                                var cartItem = cart.Items.First(i => i.Product.prod_name == item.Name);
                                cart.Items.Remove(cartItem);
                                context.CartItems.Remove(cartItem);
                                Console.WriteLine($"Removed '{item.Name}' from cart.");
                            }
                            context.SaveChanges();
                            Console.WriteLine("Cart updated. Please try checkout again.");
                            transaction.Rollback();
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Checkout cancelled.");
                            transaction.Rollback();
                            return;
                        }
                    }

                    var newOrderId = (context.Orders.Max(o => (int?)o.OrderId) ?? 0) + 1;
                    var order = new Order(newOrderId, UserAddress)
                    {
                        CustomerId = Id,
                        Status = Order.STATUS_PLACED,
                        OrderDate = DateTime.Now,
                        TrackingNumber = string.Empty
                    };

                    foreach (var item in cart.Items)
                    {
                        var product = item.Product ?? context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product == null)
                        {
                            Console.WriteLine($"Product '{item.ProductName}' not found in database.");
                            transaction.Rollback();
                            return;
                        }

                        try
                        {
                            order.AddItem(context, new OrderItem(
                                item.ProductId,
                                item.ProductName,
                                item.Price,
                                item.Quantity
                            ));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error adding item '{item.ProductName}' to order: {ex.Message}");
                            transaction.Rollback();
                            return;
                        }

                        int originalStock = product.prod_stock;
                        if (!product.UpdateStock(context, -item.Quantity))
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Failed to update stock for '{product.prod_name}'.");
                            return;
                        }
                        Console.WriteLine($"'{product.prod_name}' - Stock updated: {originalStock} → {product.prod_stock}");
                    }

                    Console.WriteLine("\n=== Order Summary ===");
                    cart.DisplayCart();
                    Console.WriteLine($"Shipping to: {UserAddress}");
                    Console.WriteLine("====================");
                    Console.Write("Confirm order? (Y/N): ");
                    if (!Console.ReadLine().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Checkout cancelled by user.");
                        transaction.Rollback();
                        return;
                    }

                    if (string.IsNullOrEmpty(paymentMethod))
                    {
                        try
                        {
                            paymentMethod = Payment.ChoosePaymentMethod();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error selecting payment method: {ex.Message}");
                            transaction.Rollback();
                            return;
                        }
                    }

                    try
                    {
                        order.ProcessPayment(context, paymentMethod);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Payment processing failed: {ex.Message}");
                        transaction.Rollback();
                        return;
                    }

                    context.Orders.Add(order);
                    OrderHistory.Add(order);

                    int pointsEarned = (int)(order.TotalAmount / 10);
                    LoyaltyPoints += pointsEarned;
                    context.Users.Update(this);
                    Console.WriteLine($"Earned {pointsEarned} loyalty points!");

                    context.CartItems.RemoveRange(cart.Items);
                    cart.Items.Clear();
                    cart.LastUpdatedDate = DateTime.Now;

                    ShoppingCart = new Cart
                    {
                        Id = cart.Id,
                        Items = cart.Items.Select(i => new CartItem
                        {
                            Id = i.Id,
                            ProductId = i.ProductId,
                            ProductName = i.ProductName,
                            Price = i.Price,
                            Quantity = i.Quantity
                        }).ToList(),
                        LastUpdatedDate = cart.LastUpdatedDate
                    };

                    context.SaveChanges();
                    transaction.Commit();

                    Console.WriteLine("\nCheckout completed successfully!");
                    ViewOrderDetails(context, order.OrderId);
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Database error: {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.Contains("Cart") && ex.Message.Contains("cannot be tracked"))
                    {
                        Console.WriteLine("Fixing duplicate Cart tracking issue...");
                        context.ChangeTracker.Clear();

                        ShoppingCart = context.Carts
                            .Include(c => c.Items)
                            .ThenInclude(i => i.Product)
                            .FirstOrDefault(c => c.Id == ShoppingCart.Id);

                        try
                        {
                            context.SaveChanges();
                            transaction.Commit();
                            Console.WriteLine("\nCheckout completed after fixing tracking issue!");
                            //Environment.Exit(0);
                        }
                        catch (Exception retryEx)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Retry failed: {retryEx.Message}");
                        }
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Checkout failed: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System error: {ex.Message}");
            }
        }

        public void ViewOrderHistory(ECommerceDbContext context, string statusFilter = null)
        {
            try
            {
                var orders = context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.Payment)
                    .Include(o => o.ShippingAddress)
                    .Where(o => o.CustomerId == Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                if (!orders.Any())
                {
                    Console.WriteLine("No orders found in your history.");
                    return;
                }

                var validStatuses = new[] { Order.STATUS_PLACED, Order.STATUS_PROCESSING, Order.STATUS_SHIPPED,
                                    Order.STATUS_DELIVERED, Order.STATUS_CANCELLED, Order.STATUS_REFUNDED };

                var ordersToDisplay = string.IsNullOrEmpty(statusFilter)
                    ? orders
                    : orders.Where(o => validStatuses.Contains(o.Status, StringComparer.OrdinalIgnoreCase)
                                     && o.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));

                if (!ordersToDisplay.Any() && !string.IsNullOrEmpty(statusFilter))
                {
                    Console.WriteLine($"No orders found with status '{statusFilter}'.");
                    Console.WriteLine("Available statuses: " + string.Join(", ", validStatuses));
                }

                Console.WriteLine($"\n=== ORDER HISTORY {(statusFilter != null ? $"({statusFilter.ToUpper()})" : "")} ===");
                Console.WriteLine($"Total Orders: {orders.Count}");

                foreach (var order in orders)
                {
                    order.DisplaySummary();
                    Console.Write("View details? (Y/N): ");
                    if (Console.ReadLine().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        order.DisplayFullDetails();
                    }
                    Console.WriteLine("---------------------");
                }

                Console.WriteLine("\n=== Summary ===");
                Console.WriteLine($"Total Spent: ${orders.Sum(o => o.TotalAmount):F2}");
                Console.WriteLine($"Loyalty Points: {LoyaltyPoints}");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error viewing order history: {ex.Message}");
            }
        }

        public void ViewOrderDetails(ECommerceDbContext context, int orderId)
        {
            try
            {
                var order = context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.Payment)
                    .Include(o => o.ShippingAddress)
                    .FirstOrDefault(o => o.OrderId == orderId && o.CustomerId == Id);

                if (order == null)
                {
                    Console.WriteLine($"Order #{orderId} not found or does not belong to you.");
                    return;
                }

                order.DisplayFullDetails();

                if (order.Status == Order.STATUS_PLACED || order.Status == Order.STATUS_PROCESSING)
                {
                    Console.WriteLine("\nOptions:");
                    Console.WriteLine("1. Cancel Order");
                    Console.WriteLine("2. Update Shipping Address");
                    Console.Write("Select an option (1-2, or press Enter to skip): ");
                    string choice = Console.ReadLine()?.Trim();

                    if (choice == "1")
                    {
                        if (CancelOrder(context, orderId))
                        {
                            Console.WriteLine("Order cancelled successfully.");
                        }
                    }
                    else if (choice == "2")
                    {
                        Console.WriteLine("Enter new shipping address:");
                        var newAddress = new Address();
                        newAddress.UpdateAddress(context);
                        order.ShippingAddress = newAddress;
                        order.ShippingAddressId = newAddress.Id;
                        context.Orders.Update(order);
                        context.SaveChanges();
                        Console.WriteLine("Shipping address updated successfully.");
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error viewing order details: {ex.Message}");
            }
        }

        public bool CancelOrder(ECommerceDbContext context, int orderId)
        {
            try
            {
                var order = context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.Payment)
                    .FirstOrDefault(o => o.OrderId == orderId && o.CustomerId == Id);

                if (order == null)
                {
                    Console.WriteLine($"Order #{orderId} not found or does not belong to you.");
                    return false;
                }

                if (order.Status == Order.STATUS_SHIPPED || order.Status == Order.STATUS_DELIVERED)
                {
                    Console.WriteLine($"Cannot cancel order #{orderId}. Current status: {order.Status}.");
                    return false;
                }

                Console.WriteLine("\n=== Order Summary ===");
                order.DisplaySummary();
                Console.WriteLine("====================");
                Console.Write("Are you sure you want to cancel this order? (Y/N): ");
                if (!Console.ReadLine().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Order cancellation cancelled by user.");
                    return false;
                }

                using var transaction = context.Database.BeginTransaction();
                try
                {
                    foreach (var item in order.Items)
                    {
                        var product = context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            if (!product.UpdateStock(context, item.Quantity))
                            {
                                transaction.Rollback();
                                Console.WriteLine($"Failed to restore stock for '{item.ProductName}'.");
                                return false;
                            }
                            Console.WriteLine($"Restored {item.Quantity} units of '{item.ProductName}' to stock.");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Product '{item.ProductName}' not found in database.");
                        }
                    }

                    order.CancelOrder(context);

                    int pointsDeducted = (int)(order.TotalAmount / 10);
                    if (pointsDeducted > 0 && LoyaltyPoints >= pointsDeducted)
                    {
                        LoyaltyPoints -= pointsDeducted;
                        context.Users.Update(this);
                        Console.WriteLine($"↩️ Deducted {pointsDeducted} loyalty points.");
                    }

                    context.SaveChanges();
                    transaction.Commit();

                    Console.WriteLine($"Order #{orderId} cancelled successfully.");
                    return true;
                }
                catch (DbUpdateException ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Database error during cancellation: {ex.InnerException?.Message ?? ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Unexpected error during cancellation: {ex.Message}");
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Cancellation failed: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System error: {ex.Message}");
                return false;
            }
        }
    }
}