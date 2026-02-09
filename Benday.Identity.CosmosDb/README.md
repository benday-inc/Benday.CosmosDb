# Benday.Identity.CosmosDb

ASP.NET Core Identity implementation using Azure Cosmos DB as the backing store. Built on top of the [Benday.CosmosDb](https://www.nuget.org/packages/Benday.CosmosDb) repository pattern library.

## Packages

| Package | Description |
|---|---|
| **Benday.Identity.CosmosDb** | Core identity models, stores, DI registration (`AddCosmosIdentity`), email sender interface, configuration, and admin seeding utility |
| **Benday.Identity.CosmosDb.UI** | Pre-built Razor Pages (Login, Logout, Register, ChangePassword, ForgotPassword, ResetPassword, ConfirmEmail, Admin User List/Edit), RedirectToLogin Blazor component, and `AddCosmosIdentityWithUI` convenience method |

## Features

- Full ASP.NET Core Identity support with Cosmos DB storage
- User management (create, update, delete, find)
- Role-based access control
- Claims-based authorization
- Account lockout protection
- Two-factor authentication (2FA) support
- External login providers (Google, Facebook, Microsoft, etc.)
- Phone number verification
- Security stamp management for token invalidation
- LINQ query support
- **One-line registration** via `AddCosmosIdentity()` (core package)
- **Admin user seeding utility** via `CosmosIdentitySeeder` (core package)
- **Pre-built account pages**: Login, Logout, AccessDenied, Register, ChangePassword, ForgotPassword, ResetPassword, ConfirmEmail (UI package)
- **Admin pages**: User List (search/paginate) and Edit User (email, lockout, roles, claims) (UI package)
- **Pluggable email sender** via `ICosmosIdentityEmailSender` with no-op default (core + UI packages)
- **Private site support** via `AllowRegistration` option (disables registration page)
- **Admin authorization** via configurable `AdminRoleName` option and `CosmosIdentityAdmin` policy
- **RedirectToLogin Blazor component** (UI package)
- **Cookie configuration** via `AddCosmosIdentityWithUI()` (UI package)

## Quick Start with UI (Recommended)

Install the UI package (includes the core package):

```bash
dotnet add package Benday.Identity.CosmosDb.UI
```

Register everything in `Program.cs`:

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

That's it. No partition key knowledge, no store registration, no cookie configuration. Login/logout pages work out of the box.

## Quick Start without UI (Web API, Console, etc.)

Install the core package only:

```bash
dotnet add package Benday.Identity.CosmosDb
```

Register stores and Identity in `Program.cs`:

```csharp
using Benday.Identity.CosmosDb;
using Benday.CosmosDb.Utilities;

var cosmosConfig = builder.Configuration.GetCosmosConfig();

builder.Services.AddCosmosIdentity(cosmosConfig)
    .AddDefaultTokenProviders();
```

This registers the Cosmos DB user/role stores and ASP.NET Core Identity, but does not configure cookies or provide login pages. Configure authentication separately as needed (e.g., JWT bearer tokens for APIs).

### Container Names

By default, both `AddCosmosIdentity` and `AddCosmosIdentityWithUI` store users and roles in the container specified by your `CosmosConfig` (i.e., `CosmosConfiguration:ContainerName` from appsettings). Users and roles coexist in the same container, separated by the hierarchical partition key's discriminator value.

You can override the container names if you want separate containers:

```csharp
builder.Services.AddCosmosIdentityWithUI(cosmosConfig,
    options =>
    {
        options.UsersContainerName = "MyUsers";
        options.RolesContainerName = "MyRoles";
    });
```

### Customization

```csharp
builder.Services.AddCosmosIdentityWithUI(cosmosConfig,
    options =>
    {
        options.CookieName = "MyApp.Auth";
        options.CookieExpiration = TimeSpan.FromDays(30);
    },
    identity =>
    {
        identity.Password.RequiredLength = 12;
        identity.Lockout.MaxFailedAccessAttempts = 3;
    });
```

All available `CosmosIdentityOptions`:

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
| `AllowRegistration` | `true` | Whether self-registration is allowed (set `false` for private sites) |
| `AdminRoleName` | `"UserAdmin"` | Role name required for admin pages |
| `RequireConfirmedEmail` | `false` | Whether email confirmation is required before sign-in |

### Blazor Server: RedirectToLogin

In your `App.razor` or route component:

```razor
<AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeRouteView>
```

### Seed Admin User

In `Program.cs` (works with both core and UI packages):

```csharp
using Benday.Identity.CosmosDb;

if (args.Contains("--seed-admin"))
{
    await CosmosIdentitySeeder.SeedAdminUserInteractive(app.Services);
    return;
}
```

Then run: `dotnet run -- --seed-admin`

### Email Sender

Password reset and email confirmation require an email sender. The library ships with a no-op default (`NoOpCosmosIdentityEmailSender`) so everything compiles and runs out of the box, but no emails are actually sent.

To enable real email delivery, implement `ICosmosIdentityEmailSender` and register it **before** calling `AddCosmosIdentityWithUI()`:

```csharp
using Benday.Identity.CosmosDb;

public class SmtpEmailSender : ICosmosIdentityEmailSender
{
    private readonly SmtpClient _client;

    public SmtpEmailSender(SmtpClient client)
    {
        _client = client;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MailMessage("noreply@yourapp.com", email, subject, htmlMessage)
        {
            IsBodyHtml = true
        };

        await _client.SendMailAsync(message);
    }
}
```

Register it in `Program.cs`:

```csharp
// Register your email sender BEFORE AddCosmosIdentityWithUI
builder.Services.AddSingleton<ICosmosIdentityEmailSender, SmtpEmailSender>();

// AddCosmosIdentityWithUI uses TryAddSingleton, so it won't overwrite yours
builder.Services.AddCosmosIdentityWithUI(cosmosConfig);
```

You can use any email provider (SMTP, SendGrid, Amazon SES, etc.) — just implement the `SendEmailAsync` method. If no custom sender is registered, the no-op default is used and password reset / email confirmation flows will silently skip sending.

### Admin Pages

The UI package includes admin pages for user management at `/Admin/Users`. These pages are protected by the `CosmosIdentityAdmin` authorization policy, which requires the role specified by `AdminRoleName` (default: `"UserAdmin"`).

**Admin features:**
- Search and paginate users
- Edit user email
- Lock/unlock user accounts
- Add/remove roles
- Add/remove claims

To grant admin access, assign the admin role to a user. The `CosmosIdentitySeeder` automatically assigns both the `"Admin"` role and your configured `AdminRoleName` role when seeding.

To customize the admin role name:

```csharp
builder.Services.AddCosmosIdentityWithUI(cosmosConfig,
    options =>
    {
        options.AdminRoleName = "SuperAdmin";
    });
```

### Private Sites

To disable self-registration (e.g., for internal or invite-only applications):

```csharp
builder.Services.AddCosmosIdentityWithUI(cosmosConfig,
    options =>
    {
        options.AllowRegistration = false;
    });
```

When `AllowRegistration` is `false`, the Register page returns a 404 and the "Create an account" link is hidden from the login page.

## Dependencies

- [Benday.CosmosDb](https://www.nuget.org/packages/Benday.CosmosDb) - Cosmos DB repository pattern library
- Microsoft.Extensions.Identity.Core

## Implemented Interfaces

### User Store (`CosmosDbUserStore`)
- `IUserStore<CosmosIdentityUser>`
- `IUserPasswordStore<CosmosIdentityUser>`
- `IUserEmailStore<CosmosIdentityUser>`
- `IUserRoleStore<CosmosIdentityUser>`
- `IUserSecurityStampStore<CosmosIdentityUser>`
- `IUserLockoutStore<CosmosIdentityUser>`
- `IUserClaimStore<CosmosIdentityUser>`
- `IUserTwoFactorStore<CosmosIdentityUser>`
- `IUserPhoneNumberStore<CosmosIdentityUser>`
- `IUserAuthenticatorKeyStore<CosmosIdentityUser>`
- `IUserTwoFactorRecoveryCodeStore<CosmosIdentityUser>`
- `IUserLoginStore<CosmosIdentityUser>`
- `IQueryableUserStore<CosmosIdentityUser>`

### Role Store (`CosmosDbRoleStore`)
- `IRoleStore<CosmosIdentityRole>`
- `IRoleClaimStore<CosmosIdentityRole>`
- `IQueryableRoleStore<CosmosIdentityRole>`

### Claims Principal Factory
- `DefaultUserClaimsPrincipalFactory` - A default implementation that adds role claims to the identity.

## Domain Models

### CosmosIdentityUser
The user entity with support for:
- Username and email (with automatic normalization)
- Password hash
- Security stamp and concurrency stamp
- Phone number with confirmation
- Two-factor authentication (authenticator key, recovery codes)
- Account lockout
- Claims collection
- External login providers

### CosmosIdentityRole
The role entity with support for:
- Role name (with automatic normalization)
- Concurrency stamp
- Claims collection

## Partition Key Strategy

All identity entities use a "SYSTEM" partition key by default, meaning all users and roles are stored in the same logical partition. This simplifies queries and works well for most applications. If you need a different partitioning strategy, you can override the `SystemOwnedItem` base class.

## Migration from v1.x

v2.0 is a breaking change. All identity classes have been renamed to avoid namespace collisions with `Microsoft.AspNetCore.Identity`:

| v1.x | v2.0 |
|---|---|
| `IdentityUser` | `CosmosIdentityUser` |
| `IdentityRole` | `CosmosIdentityRole` |
| `IdentityConstants` | `CosmosIdentityConstants` |
| `IdentityClaim` | `CosmosIdentityClaim` |
| `IdentityUserClaim` | `CosmosIdentityUserClaim` |
| `IdentityUserLogin` | `CosmosIdentityUserLogin` |

**Using aliases are no longer needed.** You can remove any `using IdentityUser = Benday.Identity.CosmosDb.IdentityUser;` directives.

## License

MIT License - see LICENSE file for details.
