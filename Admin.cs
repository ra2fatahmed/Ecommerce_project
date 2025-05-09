using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    
    public class Admin : User
    {
        public Admin()
        {
            Role = "Admin";
            IsLogin = false;
        }

        public Admin(int id, string name, string email, string password, int age, string phoneNumber, Address UserAddress)
            : base(id, name, email, password, age, phoneNumber, UserAddress)
        {
            Role = "Admin";
            IsLogin = false;
        }

        public void AddProduct(ECommerceDbContext context)
        {
            try
            {
                Console.Write("Enter Product ID: ");
                if (!int.TryParse(Console.ReadLine(), out int id) || id <= 0)
                {
                    Console.WriteLine("\nInvalid Product ID! Must be a positive number.");
                    return;
                }

                Product anyProd = context.Products.FirstOrDefault(p => p.prod_id == id);

                if (anyProd != null)
                {
                    Console.Write("Enter stock quantity to add: ");
                    if (!int.TryParse(Console.ReadLine(), out int addStock) || addStock <= 0)
                    {
                        Console.WriteLine("\nInvalid stock quantity! Must be a positive number.");
                        return;
                    }
                    anyProd.prod_stock += addStock;
                    context.SaveChanges();
                    Console.WriteLine($"\nStock updated!");
                }
                else
                {
                    Console.Write("Enter Product Name: ");
                    string name = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        Console.WriteLine("\nProduct name cannot be empty!");
                        return;
                    }

                    Console.Write("Enter Product Description: ");
                    string description = Console.ReadLine()?.Trim();

                    Console.Write("Enter Product Price: ");
                    if (!double.TryParse(Console.ReadLine(), out double price) || price <= 0)
                    {
                        Console.WriteLine("\nInvalid price! Must be a positive number.");
                        return;
                    }

                    Console.Write("Enter Stock Quantity: ");
                    if (!int.TryParse(Console.ReadLine(), out int stock) || stock < 0)
                    {
                        Console.WriteLine("\nInvalid stock quantity! Must be non-negative.");
                        return;
                    }

                    Product newProduct = new Product(id, name, description, price, stock);
                    context.Products.Add(newProduct);
                    context.SaveChanges();
                    Console.WriteLine("\nProduct added successfully!");
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError adding product: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public bool RemoveProduct(ECommerceDbContext context, int id)
        {
            try
            {
                Product productRemove = context.Products.FirstOrDefault(p => p.prod_id == id);

                if (productRemove != null)
                {
                    context.Products.Remove(productRemove);
                    context.SaveChanges();
                    Console.WriteLine($"\nProduct with ID {id} removed successfully!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"\nProduct with ID {id} not found");
                    return false;
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError removing product: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error: {ex.Message}");
                return false;
            }
        }

        public bool UpdateProduct(ECommerceDbContext context, int id)
        {
            try
            {
                Product prodUpdate = context.Products.FirstOrDefault(p => p.prod_id == id);

                if (prodUpdate != null)
                {
                    Console.WriteLine($"Updating Product ID {id}...");
                    Console.WriteLine("1. Update Name");
                    Console.WriteLine("2. Update Description");
                    Console.WriteLine("3. Update Price");
                    Console.WriteLine("4. Update Stock");
                    Console.Write("Choose: ");

                    string option = Console.ReadLine()?.Trim();
                    switch (option)
                    {
                        case "1":
                            Console.Write("Enter new product name: ");
                            string newName = Console.ReadLine()?.Trim();
                            if (string.IsNullOrWhiteSpace(newName))
                            {
                                Console.WriteLine("\nProduct name cannot be empty!");
                                return false;
                            }
                            prodUpdate.prod_name = newName;
                            break;

                        case "2":
                            Console.Write("Enter new product description: ");
                            prodUpdate.set_prod_description = Console.ReadLine()?.Trim();
                            break;

                        case "3":
                            Console.Write("Enter new price: ");
                            if (!double.TryParse(Console.ReadLine(), out double newPrice) || newPrice <= 0)
                            {
                                Console.WriteLine("\nInvalid price! Must be a positive number.");
                                return false;
                            }
                            prodUpdate.prod_price = newPrice;
                            break;

                        case "4":
                            Console.Write("Enter stock change (+ to add, - to remove): ");
                            if (!int.TryParse(Console.ReadLine(), out int quantity))
                            {
                                Console.WriteLine("\nInvalid stock quantity! Must be a number.");
                                return false;
                            }
                            if (prodUpdate.prod_stock + quantity < 0)
                            {
                                Console.WriteLine("\nCannot reduce stock below 0!");
                                return false;
                            }
                            prodUpdate.prod_stock += quantity;
                            Console.WriteLine(quantity >= 0
                                ? $"\n{quantity} units added!"
                                : $"\n{Math.Abs(quantity)} units removed!");
                            break;

                        default:
                            Console.WriteLine("\nInvalid option");
                            return false;
                    }

                    context.SaveChanges();
                    Console.WriteLine($"\nProduct ID {id} updated successfully!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"\nProduct with ID {id} not found");
                    return false;
                }
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError updating product: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error: {ex.Message}");
                return false;
            }
        }
        public bool ShipOrder(ECommerceDbContext context)
        {
            try
            {
                var pendingOrders = context.Orders
                    .Include(o => o.Customer)
                    .Where(o => o.Status == Order.STATUS_PROCESSING)
                    .ToList();

                if (!pendingOrders.Any())
                {
                    Console.WriteLine("\nNo orders ready for shipping.");
                    return false;
                }

                Console.WriteLine("Orders ready for shipping:");
                foreach (var order in pendingOrders)
                {
                    Console.WriteLine($"ID: {order.OrderId} | Customer: {order.Customer.Name} | Date: {order.OrderDate:d}");
                }

                Console.Write("\nEnter Order ID to ship: ");
                if (!int.TryParse(Console.ReadLine(), out int orderId))
                {
                    Console.WriteLine("\nInvalid Order ID!");
                    return false;
                }

                var orderToShip = context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefault(o => o.OrderId == orderId);

                if (orderToShip == null)
                {
                    Console.WriteLine("Order not found!");
                    return false;
                }

                Console.Write("Enter tracking number: ");
                string trackingNumber = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(trackingNumber))
                {
                    Console.WriteLine("Tracking number cannot be empty!");
                    return false;
                }

                orderToShip.ShipOrder(context, trackingNumber);
                Console.WriteLine($"Order {orderId} shipped successfully with tracking #: {trackingNumber}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError shipping order: {ex.Message}");
                return false;
            }
        }
        public bool MarkOrderAsDelivered(ECommerceDbContext context)
        {
            try
            {
                var shippedOrders = context.Orders
                    .Include(o => o.Customer)
                    .Where(o => o.Status == Order.STATUS_SHIPPED)
                    .ToList();

                if (!shippedOrders.Any())
                {
                    Console.WriteLine("\nNo orders ready to be marked as delivered.");
                    return false;
                }

                Console.WriteLine("\nShipped Orders (ready for delivery confirmation):");
                foreach (var order in shippedOrders)
                {
                    Console.WriteLine($"ID: {order.OrderId} | Customer: {order.Customer.Name} | Shipped on: {order.ShipmentDate:d}");
                    Console.WriteLine($"Tracking #: {order.TrackingNumber}");
                    Console.WriteLine("----------------------------------");
                }

                Console.Write("\nEnter Order ID to mark as delivered: ");
                if (!int.TryParse(Console.ReadLine(), out int orderId))
                {
                    Console.WriteLine("Invalid Order ID!");
                    return false;
                }

                var orderToMark = context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefault(o => o.OrderId == orderId);

                if (orderToMark == null)
                {
                    Console.WriteLine("Order not found!");
                    return false;
                }

                orderToMark.MarkAsDelivered(context);
                Console.WriteLine($"Order {orderId} marked as delivered successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking order as delivered: {ex.Message}");
                return false;
            }
        }
    }
}