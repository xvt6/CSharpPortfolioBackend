using System;
using DotNetEnv;
using BCrypt.Net;

namespace SharpPortfolioBackend.Utils
{
    internal class PasswordHasher
    {
        static void Main(string[] args)
        {
            // Load .env variables
            Env.Load();

            // Read admin password from env
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PWD");

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                Console.WriteLine("Error: ADMIN_PWD not set in .env");
                return;
            }

            // Generate BCrypt hash
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

            Console.WriteLine("BCrypt hash for ADMIN_PWD:");
            Console.WriteLine(passwordHash);
        }
    }
}