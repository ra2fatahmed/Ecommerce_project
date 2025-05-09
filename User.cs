using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    [Table("Users")]
    
    public  class User
    {
        [Key]
        public int Id { get; set; } 
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } 
        public int Age { get; set; }
        public string PhoneNumber { get; set; }
        public int UserAddressId { get; set; } 
        public Address UserAddress { get; set; } 
        public bool IsLogin { get; set; } = false;
        public string Role { get; set; }

        protected User()
        {
            IsLogin = false;
        }
        
        public User(int id, string name, string email, string password, int age, string phoneNumber, Address address)
        {
            Id = id;
            Name = name;
            Email = email.ToLower();
            Password = password;
            Age = age;
            PhoneNumber = phoneNumber;
            UserAddress = address;
        }

        public static User Register(ECommerceDbContext context)
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

                int id;
                while (true)
                {
                    Console.Write("Enter your ID (number only): ");
                    if (int.TryParse(Console.ReadLine(), out id))
                    {
                        if (context.Users.Any(u => u.Id == id))
                        {
                            Console.WriteLine("ID already exists. Please choose a different ID.");
                            continue;
                        }
                        break;
                    }
                    Console.WriteLine("Invalid ID. Please enter a number.");
                }

                string email;
                while (true)
                {
                    Console.Write("Enter your email (@example.com): ");
                    email = Console.ReadLine()?.Trim() ?? "";
                    if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
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

                Console.WriteLine("Enter your address details:");
                Address address = new Address();
                address.UpdateAddress(context);

                var user = new User(id, name, email, password, age, phoneNumber, address);
                context.Users.Add(user);
                context.SaveChanges();

                Console.WriteLine("User registered successfully!");
                return user;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error registering user: {ex.InnerException?.Message ?? ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return null;
            }

        }

        public bool Login(ECommerceDbContext context, string email, string password)
        {
            try
            {
                var user = context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
                if (user != null && user.Password == password) 
                {
                    IsLogin = true;
                    context.SaveChanges();
                    Console.WriteLine($"---------Welcome {Name} !---------");
                    return true;
                }
                else
                {
                    Console.WriteLine("-------Invalid email or password!-------");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return false;
            }
        }

        public void Logout(ECommerceDbContext context)
        {
            try
            {
                if (IsLogin)
                {
                    IsLogin = false;
                    context.SaveChanges();
                    Console.WriteLine($"---------Logged out done!---------");
                }
                else
                {
                    Console.WriteLine("-------Not logged in-------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
            }
        }

        public void UpdateProfile(ECommerceDbContext context)
        {
            try
            {
                Console.WriteLine($"Updating Profile for User ID {Id}...");
                Console.WriteLine("1. Update Name");
                Console.WriteLine("2. Update Email");
                Console.WriteLine("3. Update Password");
                Console.WriteLine("4. Update Age");
                Console.WriteLine("5. Update Phone Number");
                Console.WriteLine("6. Update Address");
                Console.Write("Choose an option: ");

                string option = Console.ReadLine()?.Trim();
                switch (option)
                {
                    case "1":
                        Console.Write("Enter new name: ");
                        string newName = Console.ReadLine()?.Trim() ?? "";
                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            Console.WriteLine("Name cannot be empty.");
                            return;
                        }
                        Name = newName;
                        break;

                    case "2":
                        string newEmail;
                        while (true)
                        {
                            Console.Write("Enter new email (@example.com): ");
                            newEmail = Console.ReadLine()?.Trim() ?? "";
                            if (!Regex.IsMatch(newEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                            {
                                Console.WriteLine("Invalid email format! Try again.");
                                continue;
                            }
                            if (context.Users.Any(u => u.Email == newEmail.ToLower() && u.Id != Id))
                            {
                                Console.WriteLine("Email already registered. Please use a different email.");
                                continue;
                            }
                            Email = newEmail.ToLower();
                            break;
                        }
                        break;

                    case "3":
                        string newPass;
                        while (true)
                        {
                            Console.Write("Enter new password (6-10 characters): ");
                            newPass = Console.ReadLine()?.Trim() ?? "";
                            if (newPass.Length >= 6 && newPass.Length <= 10)
                            {
                                Password = newPass; // No hashing, stored as plain text
                                break;
                            }
                            Console.WriteLine("Password must be between 6 and 10 characters! Try again.");
                        }
                        break;


                    case "4":
                        while (true)
                        {
                            Console.Write("Enter new age: ");
                            if (int.TryParse(Console.ReadLine(), out int newAge))
                            {
                                Age = newAge;
                                break;
                            }
                            Console.WriteLine("Invalid age. Please enter a number.");
                        }
                        break;

                    case "5":
                        Console.Write("Enter new phone number: ");
                        PhoneNumber = Console.ReadLine()?.Trim() ?? "";
                        break;

                    case "6":
                        UserAddress.UpdateAddress(context);
                        break;

                    default:
                        Console.WriteLine("Invalid option. No changes made.");
                        return;
                }

                context.SaveChanges();
                Console.WriteLine("Profile updated successfully!");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error updating profile: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public void DisplayUserInfo()
        {
            Console.WriteLine($"ID: {Id}\nName: {Name}\nEmail: {Email}\nAge: {Age}\nPhone: {PhoneNumber}\nAddress: {UserAddress}");
        }
    }
}