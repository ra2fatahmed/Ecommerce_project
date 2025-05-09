using System;
using System.Linq;
using e_commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceSystem
{
    class Program
    {
        private static IServiceProvider ServiceProvider { get; set; }
        private static Customer currentCustomer = null;
        private static Admin currentAdmin = null;

        static void Main(string[] args)
        {
            // Setup configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddDbContext<ECommerceDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            ServiceProvider = services.BuildServiceProvider();

            // Initialize database
            InitializeDatabase();

            // Main menu loop
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("=== E-COMMERCE SYSTEM ===");
                string[] mainMenu = { "Customer", "Admin", "Exit" };
                int mainChoice = DisplayMenu(mainMenu, "Main Menu");

                switch (mainChoice)
                {
                    case 0: CustomerMenu(); break;
                    case 1: AdminMenu(); break;
                    case 2: exit = true; break;
                }
            }
        }

        #region Database Initialization
        static void InitializeDatabase()
        {
            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

            try
            {
                if (!context.Database.CanConnect())
                {
                    Console.WriteLine("Database connection failed!");
                    return;
                }

                context.Database.Migrate();
                
                Console.WriteLine("Database initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
            }
        }

        #endregion

        #region Customer Menu System
        static void CustomerMenu()
        {
            bool backToMain = false;
            while (!backToMain)
            {
                Console.Clear();
                string[] customerMenu = { "Shopping", "My Account", "Back" };
                int customerChoice = DisplayMenu(customerMenu, "Customer Menu");

                switch (customerChoice)
                {
                    case 0: ShoppingMenu(); break;
                    case 1: MyAccountMenu(); break;
                    case 2: backToMain = true; break;
                }
            }
        }

        static void ShoppingMenu()
        {
            bool backToCustomer = false;
            while (!backToCustomer)
            {
                Console.Clear();
                string[] shoppingMenu = { "Browse Products", "View Cart", "Remove from Cart", "Checkout", "Back" };
                int shoppingChoice = DisplayMenu(shoppingMenu, "Shopping Menu");

                using var scope = ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

                switch (shoppingChoice)
                {
                    case 0: BrowseProducts(context); break;
                    case 1: ViewCart(context); break;
                    case 2: RemoveFromCart(context); break;
                    case 3: Checkout(context); break;
                    case 4: backToCustomer = true; break;
                }
            }
        }

        static void MyAccountMenu()
        {
            bool backToCustomer = false;
            while (!backToCustomer)
            {
                Console.Clear();

                string[] accountMenu;
                string menuTitle;

                if (currentCustomer != null && currentCustomer.IsLogin)
                {
                    menuTitle = $"My Account (Logged in as {currentCustomer.Name})";
                    accountMenu = new string[]
                    {
                        "View Profile", "Update Profile", "View Orders",
                        "Cancel Order", "Loyalty Points", "Logout", "Back"
                    };
                }
                else
                {
                    menuTitle = "My Account";
                    accountMenu = new string[] { "Register", "Login", "Back" };
                }

                int accountChoice = DisplayMenu(accountMenu, menuTitle);
                using var scope = ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

                switch (accountChoice)
                {
                    case 0: // Register/View Profile
                        if (currentCustomer != null && currentCustomer.IsLogin)
                        {
                            currentCustomer.DisplayUserInfo();
                            PressAnyKey();
                        }
                        else
                        {
                            RegisterCustomer(context);
                        }
                        break;

                    case 1: // Login/Update Profile
                        if (currentCustomer != null && currentCustomer.IsLogin)
                        {
                            currentCustomer.UpdateProfile(context);
                            PressAnyKey();
                        }
                        else
                        {
                            LoginCustomer(context);
                        }
                        break;

                    case 2: // View Orders/Back
                        if (currentCustomer != null && currentCustomer.IsLogin)
                        {
                            ViewOrders(context);
                        }
                        else
                        {
                            backToCustomer = true;
                        }
                        break;

                    case 3: // Cancel Order
                        if (currentCustomer != null && currentCustomer.IsLogin)
                        {
                            CancelOrder(context);
                        }
                        break;

                    case 4: // Loyalty Points
                        if (currentCustomer != null && currentCustomer.IsLogin)
                        {
                            Console.WriteLine($"You have {currentCustomer.LoyaltyPoints} loyalty points");
                            PressAnyKey();
                        }
                        break;

                    case 5: // Logout
                        if (currentCustomer != null && currentCustomer.IsLogin)
                        {
                            LogoutCustomer(context);
                            backToCustomer = true;
                        }
                        break;

                    case 6: // Back
                        backToCustomer = true;
                        break;
                }
            }
        }
        #endregion

        #region Admin Menu System
        static void AdminMenu()
        {
            bool backToMain = false;
            while (!backToMain)
            {
                Console.Clear();

                string[] adminMenu;
                string menuTitle;

                if (currentAdmin != null && currentAdmin.IsLogin)
                {
                    menuTitle = $"Admin Panel (Logged in as {currentAdmin.Name})";
                    adminMenu = new string[]
                    {
                        "My Profile", "Manage Products","Manage Orders", "Logout", "Back"
                    };
                }
                else
                {
                    menuTitle = "Admin Menu";
                    adminMenu = new string[] { "Login", "Back" };
                }

                int adminChoice = DisplayMenu(adminMenu, menuTitle);
                using var scope = ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

                switch (adminChoice)
                {
                    case 0: // Login/My Profile
                        if (currentAdmin != null && currentAdmin.IsLogin)
                        {
                            AdminProfileMenu(context);
                        }
                        else
                        {
                            LoginAdmin(context);
                        }
                        break;

                    case 1: // Manage Products/Back
                        if (currentAdmin != null && currentAdmin.IsLogin)
                        {
                            ManageProducts(context);
                        }
                        else
                        {
                            backToMain = true;
                        }
                        break;

                    case 2: // Manage Orders
                        if (currentAdmin != null && currentAdmin.IsLogin)
                        {
                            ManageOrders(context);
                        }
                        break;

                    case 3: // Logout
                        if (currentAdmin != null && currentAdmin.IsLogin)
                        {
                            LogoutAdmin(context);
                        }
                        break;

                    case 4: // Back
                        backToMain = true;
                        break;
                }
            }
        }

        static void AdminProfileMenu(ECommerceDbContext context)
        {
            bool backToAdmin = false;
            while (!backToAdmin)
            {
                Console.Clear();
                currentAdmin.DisplayUserInfo();

                string[] profileMenu = { "Update Profile", "Back" };
                int profileChoice = DisplayMenu(profileMenu, "Admin Profile");

                switch (profileChoice)
                {
                    case 0: // Update Profile
                        currentAdmin.UpdateProfile(context);
                        PressAnyKey();
                        break;

                    case 1: // Back
                        backToAdmin = true;
                        break;
                }
            }
        }

        static void ManageProducts(ECommerceDbContext context)
        {
            bool backToAdmin = false;
            while (!backToAdmin)
            {
                Console.Clear();
                string[] productMenu = { "Add Product", "Update Product", "Remove Product", "View All Products", "Back" };
                int productChoice = DisplayMenu(productMenu, "Product Management");

                switch (productChoice)
                {
                    case 0: // Add Product
                        currentAdmin.AddProduct(context);
                        PressAnyKey();
                        break;

                    case 1: // Update Product
                        UpdateProduct(context);
                        break;

                    case 2: // Remove Product
                        RemoveProduct(context);
                        break;

                    case 3: // View All Products
                        ViewAllProducts(context);
                        break;

                    case 4: // Back
                        backToAdmin = true;
                        break;
                }
            }
        }

        static void ManageOrders(ECommerceDbContext context)
        {
            bool backToAdmin = false;
            while (!backToAdmin)
            {
                Console.Clear();
                string[] orderMenu = { "View Pending Orders", "Ship Order", "Mark as Delivered", "Back" };
                int choice = DisplayMenu(orderMenu, "Order Management");

                switch (choice)
                {
                    case 0: // View Pending Orders
                        ViewPendingOrders(context);
                        break;

                    case 1: // Ship Order
                        currentAdmin.ShipOrder(context);
                        PressAnyKey();
                        break;

                    case 2: 
                        currentAdmin.MarkOrderAsDelivered(context); 
                        PressAnyKey(); 
                        break;
                    
                    case 3: // Back
                        backToAdmin = true;
                        break;
                }
            }
        }

        static void ViewPendingOrders(ECommerceDbContext context)
        {
            var pendingOrders = context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == Order.STATUS_PROCESSING)
                .ToList();

            if (!pendingOrders.Any())
            {
                Console.WriteLine("No pending orders found.");
            }
            else
            {
                Console.WriteLine("Pending Orders:");
                foreach (var order in pendingOrders)
                {
                    Console.WriteLine($"ID: {order.OrderId} | Customer: {order.Customer.Name} | Date: {order.OrderDate:d}");
                    Console.WriteLine("----------------------------------");
                }
            }
            PressAnyKey();
        }
        #endregion

        #region Helper Methods
        static void RegisterCustomer(ECommerceDbContext context)
        {
            currentCustomer = Customer.Register(context);
            if (currentCustomer != null)
            {
                currentCustomer.IsLogin = true;
                context.SaveChanges();
                Console.WriteLine("\nRegistration successful!");
                currentCustomer.DisplayUserInfo();
            }
            PressAnyKey();
        }

        static void LoginCustomer(ECommerceDbContext context)
        {
            Console.Clear();
            Console.Write("Enter email: ");
            string email = Console.ReadLine()?.Trim().ToLower();
            Console.Write("Enter password: ");
            string password = Console.ReadLine();

            var user = context.Users.OfType<Customer>()
                .FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                Console.WriteLine("Email not found!");
            }
            else if (user.Password != password)
            {
                Console.WriteLine("Incorrect password!");
            }
            else
            {
                currentCustomer = user;
                currentCustomer.IsLogin = true;
                context.Users.Update(currentCustomer);
                context.SaveChanges();
                Console.WriteLine($"Welcome {currentCustomer.Name}!");
            }
            PressAnyKey();
        }

        static void LogoutCustomer(ECommerceDbContext context)
        {
            currentCustomer.IsLogin = false;
            context.Users.Update(currentCustomer);
            context.SaveChanges();
            currentCustomer = null;
            Console.WriteLine("Logged out successfully!");
            PressAnyKey();
        }

        static void ViewOrders(ECommerceDbContext context)
        {
            Console.Write("Filter by status (leave empty for all): ");
            string statusFilter = Console.ReadLine()?.Trim();
            currentCustomer.ViewOrderHistory(context, statusFilter);
            PressAnyKey();
        }

        static void CancelOrder(ECommerceDbContext context)
        {
            Console.Write("Enter Order ID to cancel: ");
            if (int.TryParse(Console.ReadLine(), out int orderId))
            {
                currentCustomer.CancelOrder(context, orderId);
            }
            else
            {
                Console.WriteLine("Invalid Order ID!");
            }
            PressAnyKey();
        }

        static void BrowseProducts(ECommerceDbContext context)
        {
            bool backToShopping = false;
            while (!backToShopping)
            {
                Console.Clear();
                Console.WriteLine("=== Available Products ===");

                var products = context.Products.ToList();
                if (!products.Any())
                {
                    Console.WriteLine("No products available.");
                }

                foreach (var product in products)
                {
                    product.display_product();
                    Console.WriteLine("-------------------");
                }

                Console.WriteLine("\nOptions:");
                Console.WriteLine("1. Add to Cart");
                Console.WriteLine("2. Back");
                Console.Write("Choose: ");

                string choice = Console.ReadLine()?.Trim();
                switch (choice)
                {
                    case "1":
                        AddToCart(context, products);
                        break;

                    case "2":
                        backToShopping = true;
                        break;

                    default:
                        Console.WriteLine("Invalid choice!");
                        PressAnyKey();
                        break;
                }
            }
        }

        static void AddToCart(ECommerceDbContext context, List<Product> products)
        {
            Console.Write("Enter Product ID to add: ");
            if (int.TryParse(Console.ReadLine(), out int prodId))
            {
                var selected = products.FirstOrDefault(p => p.prod_id == prodId);
                if (selected != null)
                {
                    Console.Write("Enter quantity: ");
                    if (int.TryParse(Console.ReadLine(), out int qty) && qty > 0)
                    {
                        if (currentCustomer != null && currentCustomer.IsLogin)
                        {
                            try
                            {
                                currentCustomer.AddToCart(context, prodId, qty);
                                Console.WriteLine("Product added to cart successfully!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error adding to cart: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Please login to add items to cart!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid quantity!");
                    }
                }
                else
                {
                    Console.WriteLine("Product not found!");
                }
            }
            else
            {
                Console.WriteLine("Invalid Product ID!");
            }
            PressAnyKey();
        }

        static void ViewCart(ECommerceDbContext context)
        {
            if (currentCustomer != null && currentCustomer.IsLogin)
            {
                currentCustomer.ViewCart(context);
            }
            else
            {
                Console.WriteLine("Please login to view your cart!");
            }
            PressAnyKey();
        }

        static void RemoveFromCart(ECommerceDbContext context)
        {
            if (currentCustomer == null || !currentCustomer.IsLogin)
            {
                Console.WriteLine("Please login to manage your cart!");
                PressAnyKey();
                return;
            }

            currentCustomer.ViewCart(context);
            Console.Write("\nEnter Product ID to remove: ");
            if (int.TryParse(Console.ReadLine(), out int productId))
            {
                currentCustomer.RemoveFromCart(context, productId);
            }
            else
            {
                Console.WriteLine("Invalid Product ID!");
            }
            PressAnyKey();
        }

        static void Checkout(ECommerceDbContext context)
        {
            if (currentCustomer == null || !currentCustomer.IsLogin)
            {
                Console.WriteLine("Please login to checkout!");
                PressAnyKey();
                return;
            }
            currentCustomer.Checkout();
            PressAnyKey();
        }

        static void LoginAdmin(ECommerceDbContext context)
        {
            Console.Clear();
            if (currentAdmin != null && currentAdmin.IsLogin)
            {
                Console.WriteLine("Admin already logged in!");
                PressAnyKey();
                return;
            }

            Console.Write("Enter admin email: ");
            string email = Console.ReadLine()?.Trim().ToLower();
            Console.Write("Enter admin password: ");
            string password = Console.ReadLine();

            var admin = context.Users.OfType<Admin>()
                .FirstOrDefault(u => u.Email == email);

            if (admin == null)
            {
                Console.WriteLine("No admin found with that email!");
            }
            else if (admin.Password != password)
            {
                Console.WriteLine("Password verification failed!");
            }
            else
            {
                currentAdmin = admin;
                currentAdmin.IsLogin = true;
                context.Users.Update(currentAdmin);
                context.SaveChanges();
                Console.WriteLine("Admin login successful!");
            }
            PressAnyKey();
        }

        static void LogoutAdmin(ECommerceDbContext context)
        {
            currentAdmin.IsLogin = false;
            context.Users.Update(currentAdmin);
            context.SaveChanges();
            currentAdmin = null;
            Console.WriteLine("Admin logged out successfully!");
            PressAnyKey();
        }

        static void UpdateProduct(ECommerceDbContext context)
        {
            Console.Write("Enter Product ID to update: ");
            if (int.TryParse(Console.ReadLine(), out int updateId))
            {
                currentAdmin.UpdateProduct(context, updateId);
            }
            else
            {
                Console.WriteLine("Invalid Product ID!");
            }
            PressAnyKey();
        }

        static void RemoveProduct(ECommerceDbContext context)
        {
            Console.Write("Enter Product ID to remove: ");
            if (int.TryParse(Console.ReadLine(), out int removeId))
            {
                currentAdmin.RemoveProduct(context, removeId);
            }
            else
            {
                Console.WriteLine("Invalid Product ID!");
            }
            PressAnyKey();
        }

        static void ViewAllProducts(ECommerceDbContext context)
        {
            var products = context.Products.ToList();
            if (!products.Any())
            {
                Console.WriteLine("No products available.");
            }
            foreach (var product in products)
            {
                product.display_product();
                Console.WriteLine("-------------------");
            }
            PressAnyKey();
        }

        static int DisplayMenu(string[] options, string title)
        {
            int highlight = 0;
            bool selected = false;

            while (!selected)
            {
                Console.Clear();
                Console.WriteLine($"=== {title} ===\n");

                for (int i = 0; i < options.Length; i++)
                {
                    if (i == highlight)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine($"  {options[i]}  ");
                    Console.ResetColor();
                }

                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        highlight = (highlight - 1 + options.Length) % options.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        highlight = (highlight + 1) % options.Length;
                        break;
                    case ConsoleKey.Enter:
                        selected = true;
                        break;
                }
            }
            return highlight;
        }

        static void PressAnyKey()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(true);
        }
        #endregion
    }

    // Entity classes would be defined here (User, Customer, Admin, Product, Order, etc.)
    // Database context class would be defined here
}