# Benday.Identity.CosmosDb.UI

Pre-built ASP.NET Core Identity UI pages for Azure Cosmos DB. One-line setup with `AddCosmosIdentityWithUI()` gives you login, registration, password reset, email confirmation, and admin user management — all backed by Cosmos DB.

Built on top of [Benday.Identity.CosmosDb](https://www.nuget.org/packages/Benday.Identity.CosmosDb) and [Benday.CosmosDb](https://www.nuget.org/packages/Benday.CosmosDb).

## Included Pages

| Page | Path | Description |
|---|---|---|
| Login | `/Account/Login` | Username/password sign-in |
| Logout | `/Account/Logout` | Sign-out |
| Access Denied | `/Account/AccessDenied` | Unauthorized access page |
| Register | `/Account/Register` | Self-registration (can be disabled) |
| Change Password | `/Account/ChangePassword` | Authenticated password change |
| Forgot Password | `/Account/ForgotPassword` | Request a password reset email |
| Reset Password | `/Account/ResetPassword` | Reset password via emailed token |
| Confirm Email | `/Account/ConfirmEmail` | Email confirmation via emailed link |
| User List (Admin) | `/Admin/Users` | Search and paginate users |
| Edit User (Admin) | `/Admin/Users/Edit/{id}` | Edit email, lockout, roles, and claims |

## Quick Start

```bash
dotnet add package Benday.Identity.CosmosDb.UI
```

```csharp
using Benday.Identity.CosmosDb.UI;
using Benday.CosmosDb.Utilities;

var cosmosConfig = builder.Configuration.GetCosmosConfig();

builder.Services.AddCosmosIdentityWithUI(cosmosConfig);
builder.Services.AddRazorPages();

// ...

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
```

## Customization

```csharp
builder.Services.AddCosmosIdentityWithUI(cosmosConfig,
    options =>
    {
        options.CookieName = "MyApp.Auth";
        options.CookieExpiration = TimeSpan.FromDays(30);
        options.AllowRegistration = false;       // disable self-registration
        options.AdminRoleName = "SuperAdmin";    // custom admin role
        options.RequireConfirmedEmail = true;     // require email confirmation
    },
    identity =>
    {
        identity.Password.RequiredLength = 12;
        identity.Lockout.MaxFailedAccessAttempts = 3;
    });
```

### All Options

| Option | Default | Description |
|---|---|---|
| `UsersContainerName` | `CosmosConfig.ContainerName` | Container for user documents |
| `RolesContainerName` | `CosmosConfig.ContainerName` | Container for role documents |
| `CookieName` | `"Identity.Auth"` | Authentication cookie name |
| `LoginPath` | `"/Account/Login"` | Login page path |
| `LogoutPath` | `"/Account/Logout"` | Logout page path |
| `AccessDeniedPath` | `"/Account/AccessDenied"` | Access denied page path |
| `CookieExpiration` | 14 days | Cookie expiration time |
| `SlidingExpiration` | `true` | Whether to use sliding expiration |
| `AllowRegistration` | `true` | Whether self-registration is allowed |
| `AdminRoleName` | `"UserAdmin"` | Role name required for admin pages |
| `RequireConfirmedEmail` | `false` | Whether email confirmation is required before sign-in |
| `FromEmailAddress` | `""` | "From" address used by `SmtpCosmosIdentityEmailSender` |

## Password Reset & Email Confirmation

These flows require a working email sender. By default a no-op sender is registered (emails are silently skipped).

**Option 1: Built-in SMTP sender**

```csharp
builder.Services.AddSingleton(new SmtpClient("smtp.yourserver.com")
{
    Port = 587,
    Credentials = new NetworkCredential("user", "password"),
    EnableSsl = true
});

// Register BEFORE AddCosmosIdentityWithUI (it uses TryAddSingleton)
builder.Services.AddSingleton<ICosmosIdentityEmailSender, SmtpCosmosIdentityEmailSender>();

builder.Services.AddCosmosIdentityWithUI(cosmosConfig,
    options => { options.FromEmailAddress = "noreply@yourapp.com"; });
```

**Option 2: Custom sender (SendGrid, SES, etc.)**

Implement `ICosmosIdentityEmailSender` and register it before `AddCosmosIdentityWithUI()`:

```csharp
public class SendGridEmailSender : ICosmosIdentityEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Your implementation here
    }
}

builder.Services.AddSingleton<ICosmosIdentityEmailSender, SendGridEmailSender>();
builder.Services.AddCosmosIdentityWithUI(cosmosConfig);
```

## Blazor: RedirectToLogin

```razor
<AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeRouteView>
```

## Seed Admin User

```csharp
if (args.Contains("--seed-admin"))
{
    await CosmosIdentitySeeder.SeedAdminUserInteractive(app.Services);
    return;
}
```

Then run: `dotnet run -- --seed-admin`

## Admin Pages

The admin pages at `/Admin/Users` are protected by the `CosmosIdentityAdmin` authorization policy (requires the role specified by `AdminRoleName`, default `"UserAdmin"`). The `CosmosIdentitySeeder` automatically assigns this role when seeding.

## License

MIT License - see LICENSE file for details.
