# Benday.CosmosDb v5 to v6 Migration Guide

When upgrading an application from Benday.CosmosDb v5.x to v6.x, apply these changes:

## Property Renames
- `OwnerId` → `TenantId` (on all domain model classes)
- `PartitionKey` → removed; use `TenantId` directly
- `DiscriminatorValue` → `EntityType`
- `ParentDiscriminator` → `ParentEntityType`

## Interface/Class Renames
- `IOwnedItem` → `ITenantItem`
- `OwnedItemBase` → `TenantItemBase`
- `IOwnedItemRepository<T>` → `ITenantItemRepository<T>`
- `CosmosOwnedItemRepository<T>` → `CosmosTenantItemRepository<T>`
- `IOwnedItemService<T>` → `ITenantItemService<T>`
- `OwnedItemService<T>` → `TenantItemService<T>`

## Identity Library Renames
- `CosmosIdentityConstants.SystemOwnerId` → `CosmosIdentityConstants.SystemTenantId`
- `CosmosIdentityOptions.IdentityOwnerId` → `CosmosIdentityOptions.IdentityTenantId`

## Partition Key Changes
- Default partition key path changed from `/pk,/discriminator` to `/tenantId,/entityType`
- Any configuration strings referencing the old paths must be updated
- Existing Cosmos DB containers need data migration (use `cosmosmigrator migrate`)

## JSON Serialization
- Default serialization is now camelCase for all properties
- `CosmosConfig.UseCamelCase` defaults to `true`
- If your app depends on PascalCase JSON, set `UseCamelCase = false`
- Documents in existing containers need property name migration

## Class Renames
- `QueryableInfo<T>` → `QueryContext<T>`

## Method Renames
- `GetQueryable()` → `GetQueryContextAsync()` (all overloads on `CosmosRepository<T>`)
- `GetResults()` → `GetResultsAsync()` (on `CosmosRepository<T>`)
- `GetContainer()` → `GetContainerAsync()` (on `CosmosRepository<T>` — do NOT rename Cosmos SDK calls like `Database.GetContainer()` or `Database.GetContainerQueryIterator()`)
- Variable naming convention: store the result in `queryContext` (not `queryable` or `query`), then access `queryContext.Queryable` for the LINQ expression tree and `queryContext.PartitionKey` for the partition key

## Diagnostics Refactoring
- Inline diagnostic logging in `DeleteAsync`, `SaveAsync`, `GetResultsAsync`, `GetPagedAsync`, and `CosmosTenantItemRepository.GetByIdAsync` has been consolidated into three helper methods on `CosmosRepository<T>`:
  - `LogPointOperationDiagnostics(string operationName, double requestCharge, CosmosDiagnostics diagnostics, TimeSpan duration)` — for save, delete, and point-read operations
  - `LogFeedResponseDiagnostics(string queryDescription, double requestCharge, CosmosDiagnostics diagnostics, TimeSpan duration, int resultCount, string? queryText, IReadOnlyDictionary<string, object?>? parameters, PartitionKey partitionKey)` — for per-page feed iterator results; returns the cross-partition flag so callers can aggregate it into the query-total event
  - `LogQueryTotalDiagnostics(string queryDescription, double totalRequestCharge, TimeSpan totalDuration, int totalResultCount, string? queryText, IReadOnlyDictionary<string, object?>? parameters, PartitionKey partitionKey, bool isCrossPartition)` — for total RU charge and timing at query completion
- A single virtual template method on `CosmosRepository<T>` lets derived repositories hook into every diagnostics event:
  - `OnQueryDiagnostics(CosmosQueryDiagnostics diagnostics)` — fires for point operations, feed response pages, and query totals. Distinguish kinds via `diagnostics.EventKind` (`CosmosQueryEventKind.PointOperation`, `.FeedResponsePage`, `.QueryTotal`).
- **Breaking:** the previous `OnLogPointOperationDiagnostics` and `OnLogFeedResponseDiagnostics` template methods were removed. Migrate overrides to `OnQueryDiagnostics` by switching on `diagnostics.EventKind`.
- New library-side helpers on `CosmosRepository<T>` keep raw-SQL and scalar-SDK paths inside the diagnostics pipeline:
  - `GetResultsAsync(QueryDefinition, string, PartitionKey)` — raw Cosmos SQL queries.
  - `ExecuteScalarAsync<TResult>(IQueryable<T>, Func<IQueryable<T>, Task<Response<TResult>>>, string, PartitionKey, Func<TResult, int>?)` — wraps scalar SDK operators (`CountAsync`, `MaxAsync`, etc.) so they log the same request-charge and cross-partition output as list queries.
- `DiagnosticsHandler` class has been removed (was unused dead code)
- If you had custom code that overrode or called the old inline diagnostic patterns, update to use the new helper methods

## Method Parameter Renames
- Any method parameter named `ownerId` is now `tenantId`
- Any method parameter named `parentDiscriminator` is now `parentEntityType`

## How to Apply

### Option 1: Use an AI coding agent

This migration guide works as a prompt for AI coding agents like Claude Code, GitHub Copilot, or similar tools. Paste the contents of this file as a prompt and point the agent at your project. The rename rules above give the agent enough context to update your code automatically.

### Option 2: Manual migration

1. Update NuGet packages to v6.x
2. Do a find-and-replace for each rename above, starting with the longest names first
   to avoid partial replacements (e.g., rename `IOwnedItemRepository` before `IOwnedItem`)
3. Build and fix any remaining compilation errors
4. Use `cosmosmigrator migrate` to migrate existing container data
