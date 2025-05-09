using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace e_commerce
{
    public class Address
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        [MaxLength(50)]
        public string StreetNum { get; set; }

        [Required]
        [MaxLength(100)]
        public string Street { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; }

        [Required]
        [MaxLength(100)]
        public string State { get; set; }

        [Required]
        [MaxLength(100)]
        public string Country { get; set; }

        [Required]
        [MaxLength(20)]
        public string ZipCode { get; set; }

        public Address() { } 

        public Address(string streetNum, string street, string city, string state, string country, string zipCode)
        {
            StreetNum = streetNum;
            Street = street;
            City = city;
            State = state;
            Country = country;
            ZipCode = zipCode;
        }

        public void UpdateAddress(ECommerceDbContext context)
        {
            try
            {
                Console.Write("Enter your street number: ");
                string streetNum = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(streetNum))
                {
                    Console.WriteLine("\nStreet number cannot be empty.");
                    return;
                }
                StreetNum = streetNum;

                Console.Write("Enter your street: ");
                string street = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(street))
                {
                    Console.WriteLine("\nStreet cannot be empty.");
                    return;
                }
                Street = street;

                Console.Write("Enter your city: ");
                string city = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(city))
                {
                    Console.WriteLine("\nCity cannot be empty.");
                    return;
                }
                City = city;

                Console.Write("Enter your state: ");
                string state = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(state))
                {
                    Console.WriteLine("\nState cannot be empty.");
                    return;
                }
                State = state;

                Console.Write("Enter your country: ");
                string country = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(country))
                {
                    Console.WriteLine("\nCountry cannot be empty.");
                    return;
                }
                Country = country;

                while (true)
                {
                    Console.Write("Enter your zip code (numbers only, max 20 digits): ");
                    string input = Console.ReadLine()?.Trim() ?? "";
                    if (Regex.IsMatch(input, @"^\d{1,20}$"))
                    {
                        ZipCode = input;
                        break;
                    }
                    Console.WriteLine("\nInvalid zip code! Only numbers are allowed (max 20 digits).");
                }

                if (Id == 0) // New address
                {
                    context.Addresses.Add(this);
                }
                else // Existing address
                {
                    context.Addresses.Update(this);
                }

                context.SaveChanges();
                Console.WriteLine("\nAddress updated successfully!");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nError updating address: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nUnexpected error: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"{StreetNum} {Street}, {City}, {State}, {Country}, {ZipCode}";
        }
    }
}