using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.SampleApp.Api.Repositories;
using Benday.CosmosDb.SampleApp.Api.ServiceLayers;
using Benday.CosmosDb.ServiceLayers;
using Benday.CosmosDb.Utilities;
using Benday.Identity.CosmosDb;
using Benday.Identity.CosmosDb.UI;
using Microsoft.AspNetCore.Identity;

const string ADMIN_ROLE_NAME = "UserAdmin";

var builder = WebApplication.CreateBuilder(args);

// add appsettings.json and appsettings.Development.json
builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
builder.Configuration.AddJsonFile("appsettings.unversioned.json", optional: true);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var cosmosConfig = builder.Configuration.GetCosmosConfig();

// Register Cosmos Identity with UI (cookie auth, admin policy, Razor Pages)
builder.Services.AddCosmosIdentityWithUI(cosmosConfig,
    configureOptions: options =>
    {
        options.AdminRoleName = ADMIN_ROLE_NAME;
        options.EnablePasskeys = true; // Enable Passkeys (WebAuthn) support
    },
    configureIdentity: options =>
    {
        // Relax password rules for local dev
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 4;
    });

var helper = new CosmosRegistrationHelper(
    builder.Services, cosmosConfig);

helper.RegisterRepositoryAndService<Note>();
helper.RegisterParentedRepositoryAndService<Comment>();
helper.RegisterRepository<Person, IPersonRepository, CosmosDbPersonRepository>();
builder.Services.AddTransient<IPersonService, PersonService>();

helper.RegisterRepository<LookupValue, ILookupValueRepository, CosmosDbLookupValueRepository>(
    containerName: "LookupValues", withCreateStructures: true
);

builder.Services.AddTransient<ILookupValueService, LookupValueService>();

var app = builder.Build();

// Seed admin user and claim definitions on startup
await SeedAdminUserAsync(app.Services);
await SeedClaimDefinitionsAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

/// <summary>
/// Seeds default claim definitions if they don't already exist.
/// </summary>
static async Task SeedClaimDefinitionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<ICosmosDbClaimDefinitionStore>();

    var existing = await store.GetAllAsync();
    if (existing.Count > 0)
    {
        return;
    }

    var claimDefinitions = new List<CosmosIdentityClaimDefinition>
    {
        new()
        {
            ClaimType = "Department",
            Description = "The department the user belongs to",
            AllowedValues = new List<string> { "Engineering", "Sales", "Marketing", "Support", "HR" }
        },
        new()
        {
            ClaimType = "CanExport",
            Description = "Whether the user can export data",
            AllowedValues = new List<string> { "true", "false" }
        },
        new()
        {
            ClaimType = "AccessLevel",
            Description = "The user's access level",
            AllowedValues = new List<string> { "Read", "Write", "Admin" }
        }
    };

    await store.SaveAsync(claimDefinitions);
    Console.WriteLine($"Seeded {claimDefinitions.Count} claim definitions.");
}

/// <summary>
/// Seeds a default admin user (admin@test.org / password) if it doesn't already exist.
/// </summary>
static async Task SeedAdminUserAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CosmosIdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<CosmosIdentityRole>>();

    const string adminEmail = "admin@test.org";
    const string adminPassword = "password";
    const string adminRoleName = ADMIN_ROLE_NAME;

    // Ensure admin role exists
    if (await roleManager.FindByNameAsync(adminRoleName) == null)
    {
        await roleManager.CreateAsync(new CosmosIdentityRole { Name = adminRoleName });
    }

    // Create admin user if it doesn't exist
    var existingUser = await userManager.FindByEmailAsync(adminEmail);
    if (existingUser == null)
    {
        var user = new CosmosIdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User"
        };

        var result = await userManager.CreateAsync(user, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, adminRoleName);
            Console.WriteLine($"Seeded admin user: {adminEmail}");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error seeding admin user: {error.Description}");
            }
        }
    }
}
