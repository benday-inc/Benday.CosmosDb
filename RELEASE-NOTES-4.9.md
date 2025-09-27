# Benday.CosmosDb v4.9 Release Notes

## Overview
Version 4.9 introduces significant improvements to configuration management, error handling, and query performance while maintaining full backward compatibility. This release focuses on internal cleanup and enhanced developer experience.

## New Features

### 1. CosmosConfigBuilder (Recommended)
A new fluent API for configuring Cosmos DB connections, replacing the complex 12-parameter constructor:

```csharp
// Old way (now marked obsolete)
var config = new CosmosConfig(accountKey, endpoint, database, container, 
    partitionKey, createStructures, 400, false, false, true, false);

// New way - much cleaner!
var config = new CosmosConfigBuilder()
    .WithEndpoint("https://localhost:8081/")
    .WithAccountKey("your-key")
    .WithDatabase("MyDatabase", throughput: 400)
    .WithContainer("MyContainer")
    .WithPartitionKey("/pk,/discriminator", useHierarchical: true)
    .WithCreateStructures()
    .UseGatewayMode()
    .Build();

// Or with DefaultAzureCredential
var config = new CosmosConfigBuilder()
    .WithEndpoint("https://your-cosmos.documents.azure.com:443/")
    .UseDefaultAzureCredential()
    .WithDatabase("MyDatabase")
    .WithContainer("MyContainer")
    .Build();
```

### 2. Pagination Support
New `GetPagedAsync` method for efficient large result set retrieval:

```csharp
// Get first page
var firstPage = await repository.GetPagedAsync(pageSize: 50);

// Get next page using continuation token
var nextPage = await repository.GetPagedAsync(
    pageSize: 50, 
    continuationToken: firstPage.ContinuationToken);

// PagedResults includes:
// - Items: The actual results
// - ContinuationToken: For getting next page
// - HasMoreResults: Boolean indicator
// - TotalRequestCharge: RU cost tracking
```

### 3. Domain-Specific Exceptions
Replaced generic `InvalidOperationException` with meaningful exception types:

- `CosmosDbException` - Base exception for all Cosmos DB errors
- `CosmosDbItemNotFoundException` - When an item cannot be found
- `CosmosDbConfigurationException` - Configuration-related errors
- `CosmosDbBatchOperationException` - Batch operation failures with context

Example:
```csharp
try 
{
    await repository.DeleteAsync("non-existent-id");
}
catch (CosmosDbItemNotFoundException ex)
{
    Console.WriteLine($"Item {ex.ItemId} not found in {ex.ContainerName}");
}
```

## Bug Fixes

### 1. Async Method Warnings
Fixed CS1998 warnings in `BeforeSaveBatch` and `AfterSaveBatch` by properly returning `Task.CompletedTask`.

### 2. Console.WriteLine Removal
Removed all `Console.WriteLine` statements from library code. Logging now exclusively uses `ILogger`.

### 3. Access Modifier Fix
Changed `AllowBulkExecution` property setter from private to public in `CosmosConfig` to allow proper configuration.

## Breaking Changes
None. All changes are backward compatible.

## Deprecations

The following are marked as obsolete and will be removed in v5.0:
- `CosmosConfig` constructor with 12 parameters - Use `CosmosConfigBuilder` instead

## Migration Guide

### From v4.8 to v4.9

No code changes required. However, we recommend:

1. **Update configuration code** to use `CosmosConfigBuilder`:
   ```csharp
   // Replace this:
   var config = builder.Configuration.GetCosmosConfig();
   
   // Keep it, but consider explicit builder for new code:
   var config = new CosmosConfigBuilder()
       .WithEndpoint(configuration["CosmosConfiguration:Endpoint"])
       .WithAccountKey(configuration["CosmosConfiguration:AccountKey"])
       // ... etc
       .Build();
   ```

2. **Update exception handling** to use new exception types:
   ```csharp
   // Old
   catch (InvalidOperationException ex) 
   
   // New (more specific)
   catch (CosmosDbItemNotFoundException ex)
   ```

3. **Consider using pagination** for large result sets:
   ```csharp
   // Instead of GetAllAsync() for large sets
   var pagedResults = await repository.GetPagedAsync(100);
   ```

## Files Changed

### New Files
- `Benday.CosmosDb/Utilities/CosmosConfigBuilder.cs`
- `Benday.CosmosDb/Exceptions/CosmosDbException.cs`
- `Benday.CosmosDb/Repositories/PagedResults.cs`

### Modified Files
- `Benday.CosmosDb/Utilities/CosmosConfig.cs` - Added obsolete attribute
- `Benday.CosmosDb/Utilities/CosmosClientOptionsUtilities.cs` - Removed Console.WriteLine
- `Benday.CosmosDb/Repositories/CosmosRepository.cs` - Added pagination, fixed async warnings, updated exceptions
- `Benday.CosmosDb/Repositories/IRepository.cs` - Added GetPagedAsync method

## Known Issues
- Integration tests require CosmosDB emulator running on localhost:8081
- Obsolete warnings will appear when using old constructor (intentional)

## Next Steps (v5.0 Consideration)

Based on this cleanup, v5.0 would only be needed if you want to:
- Remove deprecated methods completely
- Drop netstandard2.1 support for .NET 6+ only
- Redesign partition key configuration fundamentally
- Change repository interface signatures

For now, continuing with 4.x releases is recommended.