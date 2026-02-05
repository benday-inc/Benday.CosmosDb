# Benday.Identity.CosmosDb

ASP.NET Core Identity implementation using Azure Cosmos DB as the backing store. Built on top of the [Benday.CosmosDb](https://www.nuget.org/packages/Benday.CosmosDb) repository pattern library.

## Packages

| Package | Description |
|---|---|
| **Benday.Identity.CosmosDb** | Core identity models and stores |
| **Benday.Identity.CosmosDb.UI** | Registration extension method, pre-built Razor Pages (Login/Logout/AccessDenied), RedirectToLogin Blazor component, and admin seeding utility |

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
- **One-line registration** via `AddCosmosIdentity()` (in UI package)
- **Pre-built Login/Logout/AccessDenied pages** (in UI package)
- **RedirectToLogin Blazor component** (in UI package)
- **Admin user seeding utility** (in UI package)

## Quick Start (Recommended)

Install both packages:

```bash
dotnet add package Benday.Identity.CosmosDb.UI
```

Register everything in `Program.cs`:

```csharp
using Benday.Identity.CosmosDb.UI;
using Benday.CosmosDb.Utilities;

var cosmosConfig = builder.Configuration.GetCosmosConfig();

builder.Services.AddCosmosIdentity(cosmosConfig);
builder.Services.AddRazorPages();

// ...

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
```

That's it. No partition key knowledge, no store registration, no cookie configuration. Login/logout pages work out of the box.

### Customization

```csharp
builder.Services.AddCosmosIdentity(cosmosConfig,
    options =>
    {
        options.CookieName = "MyApp.Auth";
        options.UsersContainerName = "AppUsers";
        options.CookieExpiration = TimeSpan.FromDays(30);
    },
    identity =>
    {
        identity.Password.RequiredLength = 12;
        identity.Lockout.MaxFailedAccessAttempts = 3;
    })
    .AddDefaultTokenProviders();
```

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

In `Program.cs`:

```csharp
if (args.Contains("--seed-admin"))
{
    await CosmosIdentitySeeder.SeedAdminUserInteractive(app.Services);
    return;
}
```

Then run: `dotnet run -- --seed-admin`

## Core Package Only

If you want just the models and stores without the UI package:

```bash
dotnet add package Benday.Identity.CosmosDb
```

You'll need to register the stores, identity, and cookie configuration manually.

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
