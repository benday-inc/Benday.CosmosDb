# Benday.Identity.CosmosDb.UI

Pre-built ASP.NET Core Identity UI pages for Azure Cosmos DB. One-line setup with `AddCosmosIdentityWithUI()` gives you login, registration, password reset, email confirmation, passkey management, account management, and a full admin dashboard — all backed by Cosmos DB.

Built on top of [Benday.Identity.CosmosDb](https://www.nuget.org/packages/Benday.Identity.CosmosDb) and [Benday.CosmosDb](https://www.nuget.org/packages/Benday.CosmosDb).

## Included Pages

### Authentication
| Page | Path | Description |
|---|---|---|
| Login | `/Account/Login` | Username/password + passkey sign-in |
| Logout | `/Account/Logout` | Sign-out |
| Access Denied | `/Account/AccessDenied` | Unauthorized access page |
| Register | `/Account/Register` | Self-registration (can be disabled) |
| Forgot Password | `/Account/ForgotPassword` | Request a password reset email |
| Reset Password | `/Account/ResetPassword` | Reset password via emailed token |
| Confirm Email | `/Account/ConfirmEmail` | Email confirmation via emailed link |

### Account Management
| Page | Path | Description |
|---|---|---|
| My Account | `/Account/MyAccount` | Hub page linking to all account features |
| Edit Profile | `/Account/EditProfile` | Update first name, last name, phone number |
| Change Password | `/Account/ChangePassword` | Authenticated password change |
| Manage Passkeys | `/Account/ManagePasskeys` | Add/remove passkeys for passwordless sign-in |

### Admin Dashboard (requires `CosmosIdentityAdmin` policy)
| Page | Path | Description |
|---|---|---|
| Admin Dashboard | `/Account/Admin` | Hub page for all admin features |
| Users | `/Account/AdminUsers` | Search and list users |
| Create User | `/Account/AdminUserCreate` | Create a new user account |
| Edit User | `/Account/AdminUserEdit?id=` | Edit profile, lock/unlock, reset password, delete |
| User Roles | `/Account/AdminUserRoles?id=` | Assign/remove roles for a user |
| User Claims | `/Account/AdminUserClaims?id=` | Assign/remove claims using claim definitions |
| Roles | `/Account/AdminRoles` | Create and delete security roles |
| Claim Definitions | `/Account/AdminClaimDefinitions` | Define claim types and allowed values |
| Edit Claim Def | `/Account/AdminClaimDefinitionEdit?id=` | Create/edit a claim definition |

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
| `ShowRememberMe` | `true` | Whether to show "Remember me" checkbox on login |
| `RememberMeDefaultValue` | `true` | Default checked state of "Remember me" |
| `FromEmailAddress` | `""` | "From" address used by `SmtpCosmosIdentityEmailSender` |
| `EnablePasskeys` | `true` | Whether passkey (WebAuthn) authentication is enabled |
| `PasskeyServerDomain` | `null` | WebAuthn Relying Party ID (domain) |
| `ClaimDefinitionsContainerName` | `CosmosConfig.ContainerName` | Container for claim definition documents |

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

The admin dashboard at `/Account/Admin` is protected by the `CosmosIdentityAdmin` authorization policy (requires the role specified by `AdminRoleName`, default `"UserAdmin"`). The `CosmosIdentitySeeder` automatically assigns this role when seeding.

The admin section includes full user management (create, edit, lock/unlock, reset password, delete), role management, claim definition management (with optional allowed values), and user role/claim assignment.

**Navigation flow:** Navbar -> My Account (`/Account/MyAccount`) -> Admin Dashboard (shown only for admins) -> Users / Roles / Claim Definitions

## License

MIT License - see LICENSE file for details.
