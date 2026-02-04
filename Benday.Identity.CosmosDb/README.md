# Benday.Identity.CosmosDb

ASP.NET Core Identity implementation using Azure Cosmos DB as the backing store. Built on top of the [Benday.CosmosDb](https://www.nuget.org/packages/Benday.CosmosDb) repository pattern library.

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

## Installation

```bash
dotnet add package Benday.Identity.CosmosDb
```

## Dependencies

- [Benday.CosmosDb](https://www.nuget.org/packages/Benday.CosmosDb) - Cosmos DB repository pattern library
- Microsoft.Extensions.Identity.Core

## Implemented Interfaces

### User Store (`CosmosDbUserStore`)
- `IUserStore<IdentityUser>`
- `IUserPasswordStore<IdentityUser>`
- `IUserEmailStore<IdentityUser>`
- `IUserRoleStore<IdentityUser>`
- `IUserSecurityStampStore<IdentityUser>`
- `IUserLockoutStore<IdentityUser>`
- `IUserClaimStore<IdentityUser>`
- `IUserTwoFactorStore<IdentityUser>`
- `IUserPhoneNumberStore<IdentityUser>`
- `IUserAuthenticatorKeyStore<IdentityUser>`
- `IUserTwoFactorRecoveryCodeStore<IdentityUser>`
- `IUserLoginStore<IdentityUser>`
- `IQueryableUserStore<IdentityUser>`

### Role Store (`CosmosDbRoleStore`)
- `IRoleStore<IdentityRole>`
- `IRoleClaimStore<IdentityRole>`
- `IQueryableRoleStore<IdentityRole>`

### Claims Principal Factory
- `DefaultUserClaimsPrincipalFactory` - A default implementation that adds role claims to the identity. Override this class to customize claims generation for your application.

## Usage

Register the identity stores in your `Program.cs` or startup configuration:

```csharp
using Benday.Identity.CosmosDb;
using Benday.CosmosDb.Utilities;

// Configure Cosmos DB
var cosmosConfig = new CosmosConfigBuilder()
    .UseLocalEmulator()
    .WithDatabase("YourDatabase")
    .Build();

// Register Cosmos client
services.AddSingleton(sp =>
{
    var options = CosmosClientOptionsUtilities.GetCosmosClientOptions(cosmosConfig);
    return new CosmosClient(cosmosConfig.ConnectionString, options);
});

// Register User Store
services.Configure<CosmosRepositoryOptions<IdentityUser>>(options =>
{
    options.DatabaseName = cosmosConfig.DatabaseName;
    options.ContainerName = "Users";
    options.PartitionKeyPath = "/ownerId";
});
services.AddScoped<IUserStore<IdentityUser>, CosmosDbUserStore>();
services.AddScoped<ICosmosDbUserStore, CosmosDbUserStore>();

// Register Role Store
services.Configure<CosmosRepositoryOptions<IdentityRole>>(options =>
{
    options.DatabaseName = cosmosConfig.DatabaseName;
    options.ContainerName = "Roles";
    options.PartitionKeyPath = "/ownerId";
});
services.AddScoped<IRoleStore<IdentityRole>, CosmosDbRoleStore>();

// Register Identity with default claims principal factory
services.AddIdentity<IdentityUser, IdentityRole>()
    .AddDefaultTokenProviders();

services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, DefaultUserClaimsPrincipalFactory>();
```

## Domain Models

### IdentityUser
The user entity with support for:
- Username and email (with automatic normalization)
- Password hash
- Security stamp and concurrency stamp
- Phone number with confirmation
- Two-factor authentication (authenticator key, recovery codes)
- Account lockout
- Claims collection
- External login providers

### IdentityRole
The role entity with support for:
- Role name (with automatic normalization)
- Concurrency stamp
- Claims collection

## Partition Key Strategy

All identity entities use a "SYSTEM" partition key by default, meaning all users and roles are stored in the same logical partition. This simplifies queries and works well for most applications. If you need a different partitioning strategy, you can override the `SystemOwnedItem` base class.

## License

MIT License - see LICENSE file for details.
