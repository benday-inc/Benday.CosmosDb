# Benday.CosmosDb

A collection of classes for implementing the domain model and repository patterns with [Azure Cosmos Db](https://azure.microsoft.com/en-us/products/cosmos-db).
These classes are built using the [Azure Cosmos DB libraries for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/cosmosdb?view=azure-dotnet) and aim to 
simplify using hierarchical partition keys and to make sure that query operations are created to use those partition keys correctly.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
https://www.slidespeaker.ai  
info@benday.com  
YouTube: https://www.youtube.com/@_benday  

## Key features

* Interfaces and base classes for implementing the [repository pattern](https://martinfowler.com/eaaCatalog/repository.html) with CosmosDb
* Interfaces and base classes for implementing the [domain model pattern](https://en.wikipedia.org/wiki/Domain_model) with CosmosDb
* Service layer abstractions (`IOwnedItemService`, `IParentedItemService`)
* Support for parent-child entity relationships with `ParentedItemBase` and `IParentedItem`
* Help you to write LINQ queries against CosmosDb without having to worry whether you're using the right partition keys
* Support for configuring repositories for use in ASP.NET Core projects
* Support for [hierarchical partition keys](https://learn.microsoft.com/en-us/azure/cosmos-db/hierarchical-partition-keys)
* Logging of query performance and [request units](https://learn.microsoft.com/en-us/azure/cosmos-db/request-units)
* Detect and warn when you have cross-partition queries
* Helper classes and methods for registering types and handling connection configuration
* Ultra-simple configuration for Azure Cosmos DB Linux emulator
* Optimistic concurrency control using ETags

## Quick Start

### For Azure Cosmos DB Emulator (Development)
```json
{
  "CosmosConfiguration": {
    "UseEmulator": true
  }
}
```
That's it! See [EMULATOR-SETUP.md](EMULATOR-SETUP.md) for complete emulator configuration guide.

### For Production

**Option 1: Using appsettings.json**
```json
{
  "CosmosConfiguration": {
    "DatabaseName": "ProductionDb",
    "ContainerName": "ProductionContainer",
    "CreateStructures": false,
    "PartitionKey": "/pk,/discriminator",
    "HierarchicalPartitionKey": true,
    "Endpoint": "https://your-cosmos.documents.azure.com:443/",
    "UseDefaultAzureCredential": true
  }
}
```

**Option 2: Using CosmosConfigBuilder**
```csharp
var config = new CosmosConfigBuilder()
    .WithEndpoint("https://your-cosmos.documents.azure.com:443/")
    .UseDefaultAzureCredential()
    .WithDatabase("ProductionDb")
    .WithContainer("ProductionContainer")
    .Build();
```

## Quick Example

```csharp
// Configure in Program.cs / Startup.cs
var cosmosConfig = builder.Configuration.GetCosmosConfig();
var cosmosBuilder = new CosmosRegistrationHelper(builder.Services, cosmosConfig);

// Register simple owned item
cosmosBuilder.RegisterRepositoryAndService<Note>();

// Register parented item with parent-child relationships
cosmosBuilder.RegisterParentedRepositoryAndService<Comment>();

// Use in your services
public class CommentService
{
    private readonly IParentedItemService<Comment> _commentService;

    public CommentService(IParentedItemService<Comment> commentService)
    {
        _commentService = commentService;
    }

    public async Task<IEnumerable<Comment>> GetCommentsForNote(string ownerId, string noteId)
    {
        // Query comments by parent ID with type discrimination
        return await _commentService.GetAllByParentIdAsync(ownerId, noteId, "Note");
    }
}
```

### Custom Configuration Per Repository

You can register repositories with custom configuration values that override the defaults. This is useful when you need to store certain entity types in separate containers:

```csharp
// Register LookupValue entities in a separate "LookupValues" container
cosmosBuilder.RegisterRepository<LookupValue, ILookupValueRepository, CosmosDbLookupValueRepository>(
    containerName: "LookupValues",
    withCreateStructures: true
);
```

Available configuration overrides:
- `connectionString` - Use a different Cosmos DB endpoint
- `databaseName` - Store in a different database
- `containerName` - Store in a different container
- `partitionKey` - Use a different partition key path
- `useHierarchicalPartitionKey` - Override hierarchical partition key setting
- `useDefaultAzureCredential` - Override authentication method
- `withCreateStructures` - Override auto-creation of database/container

Any parameter left as `null` will use the default value from the `CosmosRegistrationHelper` instance.

## Sample Application

A complete working sample application is included in this repository demonstrating all major features:

- **Person Entity** - Demonstrates `OwnedItemBase` with custom repository and service layer implementations. Shows complex domain models with nested Address objects.
- **Note Entity** - Simple `OwnedItemBase` implementation using default repository and service registrations. Serves as parent entity for Comments.
- **Comment Entity** - Demonstrates `ParentedItemBase` pattern showing parent-child relationships with `ParentId` and `ParentDiscriminator` for type-safe queries.
- **LookupValue Entity** - Demonstrates custom repository configuration to store entities in a separate Cosmos DB container using the `RegisterRepository` overload with configuration options.

To run the sample application:
1. Start the Azure Cosmos DB Emulator (see [EMULATOR-SETUP.md](EMULATOR-SETUP.md))
2. Run `Benday.CosmosDb.SampleApp.WebUi`
3. Explore the different entity patterns and their implementations

## Resources

📚 [Full Documentation](https://benday-inc.github.io/Benday.CosmosDb/)
💻 [Sample Application](Benday.CosmosDb.SampleApp.WebUi) - Working examples of all patterns
📦 [NuGet Package](https://www.nuget.org/packages/Benday.CosmosDb/)
🐙 [Source Code](https://github.com/benday-inc/Benday.CosmosDb)
📖 [Repository API Documentation](api/Benday.CosmosDb.Repositories.html)
📖 [Domain Model API Documentation](api/Benday.CosmosDb.DomainModels.html)

## Feedback & Contributions

Got ideas for CosmosDb functionality that you'd like to see? Found a bug? Let us know by [submitting an issue](https://github.com/benday-inc/Benday.CosmosDb/issues). Want to contribute? Submit a pull request.
