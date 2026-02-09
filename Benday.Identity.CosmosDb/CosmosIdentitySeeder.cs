using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.Identity.CosmosDb;

/// <summary>
/// Utility for seeding an initial admin user via the console.
/// </summary>
public static class CosmosIdentitySeeder
{
    /// <summary>
    /// Interactively prompts for email and password on the console,
    /// then creates an admin user with the "Admin" role.
    /// </summary>
    /// <param name="services">The application's service provider (e.g., app.Services).</param>
    public static async Task SeedAdminUserInteractive(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CosmosIdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<CosmosIdentityRole>>();

        Console.Write("Admin email: ");
        var email = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine("Error: Email cannot be empty.");
            return;
        }

        Console.Write("Password: ");
        var password = ReadPasswordFromConsole();
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Error: Password cannot be empty.");
            return;
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            Console.WriteLine($"Error: A user with email '{email}' already exists.");
            return;
        }

        // Create the user
        var user = new CosmosIdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            Console.WriteLine("Failed to create user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error.Description}");
            }
            return;
        }

        // Ensure Admin role exists
        if (await roleManager.FindByNameAsync("Admin") == null)
        {
            var roleResult = await roleManager.CreateAsync(new CosmosIdentityRole { Name = "Admin" });
            if (!roleResult.Succeeded)
            {
                Console.WriteLine("Warning: Failed to create Admin role:");
                foreach (var error in roleResult.Errors)
                {
                    Console.WriteLine($"  - {error.Description}");
                }
            }
        }

        // Add user to Admin role
        await userManager.AddToRoleAsync(user, "Admin");

        Console.WriteLine($"Admin user '{email}' created successfully.");
    }

    private static string ReadPasswordFromConsole()
    {
        var password = string.Empty;

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password = password[..^1];
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                Console.Write('*');
            }
        }

        return password;
    }
}
