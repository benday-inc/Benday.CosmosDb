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

# Run tests for a specific project
dotnet test Benday.CosmosDb.UnitTests/Benday.CosmosDb.UnitTests.csproj
```

## Architecture Overview

This is a .NET library for implementing domain model and repository patterns with Azure Cosmos DB, focusing on hierarchical partition keys and query optimization.

### Core Components

**Benday.CosmosDb** - Main library (targets netstandard2.1)
- `DomainModels/` - Base classes and interfaces for domain entities (`ICosmosIdentity`, `CosmosIdentityBase`, `IOwnedItem`, `OwnedItemBase`)
- `Repositories/` - Repository pattern implementations (`CosmosRepository<T>`, `CosmosOwnedItemRepository<T>`)
- `ServiceLayers/` - Service layer abstractions (`IOwnedItemService<T>`, `OwnedItemService<T>`)
- `Utilities/` - Helper classes for configuration and registration (`CosmosRegistrationHelper`, `CosmosConfig`, `CosmosDbUtilities`)

### Key Patterns

**Repository Pattern**: All repositories inherit from `CosmosRepository<T>` which provides:
- CRUD operations with partition key management
- LINQ query support with automatic partition key inclusion
- Batch operations with configurable batch size
- Request unit tracking and cross-partition query detection
- Optimistic concurrency control using ETags

**Domain Model Pattern**: Entities implement `ICosmosIdentity` which provides:
- Required properties: `Id`, `PartitionKey`, `Discriminator`, `_etag`
- Support for both flat and hierarchical partition keys

**Configuration**: The library uses `CosmosConfig` for connection settings:
- Supports both connection string and DefaultAzureCredential authentication
- Configurable for local emulator (default key included)
- Options for gateway mode, bulk execution, and database throughput

### Sample Applications

- **Benday.CosmosDb.SampleApp.Api** - Domain models and repositories for sample entities (Person, Note)
- **Benday.CosmosDb.SampleApp.WebUi** - ASP.NET Core MVC app demonstrating usage
- **Benday.CosmosDb.SampleApp.Tests** - Integration tests (require Cosmos DB emulator running on localhost:8081)

## Important Notes

- Integration tests require Azure Cosmos DB Emulator running locally
- Default emulator key is used in appsettings.json
- The library defaults to non-hierarchical partition keys unless `UseHierarchicalPartitionKey` is set to true
- Template methods `BeforeSaveBatch` and `AfterSaveBatch` are available for batch save customization