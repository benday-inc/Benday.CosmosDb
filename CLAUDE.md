# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run tests without building
dotnet test --no-build

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run unit tests only (no emulator needed)
dotnet test Benday.CosmosDb.UnitTests/Benday.CosmosDb.UnitTests.csproj
dotnet test Benday.Identity.CosmosDb.UnitTests/Benday.Identity.CosmosDb.UnitTests.csproj
dotnet test Benday.CosmosDb.MigrationTool.Tests/Benday.CosmosDb.MigrationTool.Tests.csproj

# Integration tests (require Cosmos DB emulator on localhost:8081)
dotnet test Benday.CosmosDb.SampleApp.Tests/Benday.CosmosDb.SampleApp.Tests.csproj
dotnet test Benday.Identity.CosmosDb.IntegrationTests/Benday.Identity.CosmosDb.IntegrationTests.csproj
```

## Solution Structure

| Project | Target | Purpose |
|---------|--------|---------|
| **Benday.CosmosDb** | netstandard2.1 | Core library — repository pattern, domain models, configuration |
| **Benday.Identity.CosmosDb** | net10.0 | ASP.NET Core Identity stores backed by Cosmos DB |
| **Benday.Identity.CosmosDb.UI** | net10.0 | Razor class library — login, registration, admin pages, passkey support |
| **Benday.CosmosDb.MigrationTool** | net10.0 | CLI tool (`cosmosmigrator`) for migrating v5→v6 data schemas |
| **Benday.CosmosDb.SampleApp.Api** | net10.0 | Sample domain models and repositories (Person, Note, Comment, LookupValue) |
| **Benday.CosmosDb.SampleApp.WebUi** | net10.0 | Sample ASP.NET Core MVC app |

Unit test projects multi-target net9.0 and net10.0. All packages are version 6.0.10 with `GeneratePackageOnBuild`. The Identity packages now share the same version line as the core package; they jumped from 3.x to 6.x to align release cadence.

## Architecture Overview

This is a .NET library for implementing domain model and repository patterns with Azure Cosmos DB, focusing on hierarchical partition keys and query optimization.

### Domain Model Hierarchy

```
ICosmosIdentity (Id, TenantId, EntityType, _etag)
  └─ CosmosIdentityBase (abstract — provides defaults, Timestamp)
       └─ TenantItemBase (abstract — adds IBlobOwner, GetBlobPrefix())
            └─ ParentedItemBase (abstract — adds ParentId, ParentEntityType)
```

Entities implement `ICosmosIdentity`. Each entity class overrides `GetEntityTypeName()` to return its discriminator value. Default partition key path: `/tenantId,/entityType` (hierarchical). CamelCase JSON serialization by default.

### Repository & Service Layers

All repositories inherit from `CosmosRepository<T>` which provides CRUD, LINQ queries with automatic partition key inclusion, batch operations, RU tracking, cross-partition query detection, paged queries with continuation tokens, and optimistic concurrency via ETags.

**DI registration** uses `CosmosRegistrationHelper`:
```csharp
var cosmosConfig = builder.Configuration.GetCosmosConfig();
var helper = new CosmosRegistrationHelper(builder.Services, cosmosConfig);
helper.RegisterRepositoryAndService<Note>();                    // simple entity
helper.RegisterParentedRepositoryAndService<Comment>();         // parent-child
helper.RegisterRepository<LookupValue, ILookupValueRepository, // custom repo, separate container
    CosmosDbLookupValueRepository>(containerName: "LookupValues");
```

Per-repository overrides available: `connectionString`, `databaseName`, `containerName`, `partitionKey`, `useHierarchicalPartitionKey`, `useDefaultAzureCredential`, `withCreateStructures`.

Template methods `BeforeSaveBatch` and `AfterSaveBatch` are available for batch save customization.

### Query Diagnostics

Every point operation, feed-iterator page, and query total emits a `CosmosQueryDiagnostics` event (event kinds: `PointOperation`, `FeedResponsePage`, `QueryTotal`). Two parallel channels deliver them — both fire for every event:

- `OnQueryDiagnostics(CosmosQueryDiagnostics)` — virtual hook on `CosmosRepository<T>` for per-repository handling.
- `ICosmosQueryLogSink` — app-wide singleton resolved from DI. Default is `NoOpCosmosQueryLogSink`; the library ships `FileCosmosQueryLogSink` (NDJSON via background queue, configured by `CosmosFileLogSinkOptions`). Register custom sinks via `helper.WithQueryLogSink<TSink>()`.

Per-entity options live in `CosmosDiagnosticsRegistry`, configured fluently via `helper.ConfigureDiagnostics<TEntity>(o => ...)` or `helper.ConfigureDiagnosticsDefault(o => ...)`. The first opt-in flag is `CaptureIndexMetrics` — when on, the repository sets `PopulateIndexMetrics = true` on every `QueryRequestOptions` it builds and copies `FeedResponse.IndexMetrics` (the SDK's formatted index-utilization string) into the `IndexMetrics` field of the diagnostics event. Off by default because of measurable RU/latency overhead.

Repository constructors accept optional `ICosmosQueryLogSink? sink = null` and `CosmosDiagnosticsRegistry? diagnosticsRegistry = null` trailing parameters. Derived classes that pass `base(...)` explicitly must forward both. DI resolves the registered singletons automatically.

### Configuration

`CosmosConfig` for connection settings — supports connection string, `DefaultAzureCredential`, emulator mode (`UseEmulator: true`), gateway mode, bulk execution, and database throughput. `CosmosConfigBuilder` provides a fluent builder alternative. Config section name: `CosmosConfiguration`.

### Identity Library (Benday.Identity.CosmosDb)

Complete ASP.NET Core Identity implementation for Cosmos DB:
- `CosmosDbUserStore` implements 14 Identity interfaces (password, email, roles, claims, 2FA, passkeys/WebAuthn, logins)
- `CosmosDbRoleStore` — role management with claims
- `CosmosDbClaimDefinitionStore` — custom claim type definitions with allowed values
- All identity classes prefixed with `Cosmos` to avoid collisions with Microsoft.AspNetCore.Identity types
- `CosmosIdentityOptions` configures container names, cookie settings, passkey settings, admin role, registration toggles
- DI setup: `AddCosmosIdentityStores()` (stores only) or `AddCosmosIdentity()` (full Identity framework) or `AddCosmosIdentityWithUI()` (includes Razor Pages)

**Identity UI** is a Razor class library with account pages (login, register, change password, forgot/reset password, passkey management) and admin pages (user CRUD, role management, claim definitions). Login/logout MUST be Razor Pages (not Blazor) for cookie auth.

### Migration Tool

`cosmosmigrator` is a .NET tool (PackAsTool=true) built on `Benday.CommandsFramework`. Migrates Cosmos DB containers from v5 schema (`pk`/`discriminator`) to v6 schema (`tenantId`/`entityType`) with camelCase transformation. Key components: `DocumentTransformer` (JSON-level), `AdaptiveConcurrencyController` (429 throttling), `MigrationRunner`. Supports dry-run and validate-only modes.

## CI/CD

Two GitHub Actions workflows in `.github/workflows/`:

- **Benday.CosmosDb.yml**: unit tests → integration tests (Cosmos DB emulator + Azurite containers) → NuGet deploy
- **Benday.Identity.CosmosDb.yml**: build + unit tests → NuGet deploy (pushes both Identity and Identity.UI packages)

Both deploy to nuget.org using `NUGET_API_KEY` secret with `nuget-deploy` environment approval gate.

## Important Notes

- Integration tests require Azure Cosmos DB Linux emulator on localhost:8081 (see EMULATOR-SETUP.md)
- `AzureCosmosDisableNewtonsoftJsonCheck` MSBuild property required in any project referencing Benday.CosmosDb
- The library defaults to non-hierarchical partition keys unless `UseHierarchicalPartitionKey` is set to true
- Blob attachment support via `IBlobOwner` on `TenantItemBase` — works with Benday.BlobStorage package
- v5→v6 migration guide in MIGRATION-v6.md (can be used as AI agent prompt)