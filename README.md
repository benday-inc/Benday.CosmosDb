# Benday.CosmosDb

A collection of classes for implementing the domain model and repository patterns with [Azure Cosmos DB](https://azure.microsoft.com/en-us/products/cosmos-db).
These classes are built using the [Azure Cosmos DB libraries for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/cosmosdb?view=azure-dotnet) and aim to 
simplify using hierarchical partition keys and to make sure that query operations are created to use those partition keys correctly.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
https://www.honestcheetah.com  
info@benday.com  
YouTube: https://www.youtube.com/@_benday  

## Key Features

* Interfaces and base classes for implementing the [repository pattern](https://martinfowler.com/eaaCatalog/repository.html) with Cosmos DB
* Interfaces and base classes for implementing the [domain model pattern](https://en.wikipedia.org/wiki/Domain_model) with Cosmos DB
* Service layer abstractions (`ITenantItemService`, `IParentedItemService`)
* Support for parent-child entity relationships with `ParentedItemBase` and `IParentedItem`
* **Blob attachment support** via `IBlobOwner` -- connect Cosmos DB documents to files in Azure Blob Storage using [Benday.BlobStorage](https://www.nuget.org/packages/Benday.BlobStorage)
* Help you to write LINQ queries against Cosmos DB without having to worry whether you're using the right partition keys
* Support for configuring repositories for use in ASP.NET Core projects
* Support for [hierarchical partition keys](https://learn.microsoft.com/en-us/azure/cosmos-db/hierarchical-partition-keys)
* Bulk operations with throttling and retry logic (`SaveAllAsync`, `DeleteAllByTenantIdAsync`)
* Paged query results with continuation tokens (`GetPagedAsync`)
* Shared interface contracts via `Benday.Common.Interfaces` (`IAsyncTenantRepository`, `IBlobOwner`, `IParentedItem`)
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
    "PartitionKey": "/tenantId,/entityType",
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

// Register simple tenant item
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

    public async Task<IEnumerable<Comment>> GetCommentsForNote(string tenantId, string noteId)
    {
        // Query comments by parent ID with entity type filtering
        return await _commentService.GetAllByParentIdAsync(tenantId, noteId, "Note");
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

## Blob Attachments with IBlobOwner and BlobBridge

Cosmos DB entities that inherit from `TenantItemBase` automatically implement `IBlobOwner` from [Benday.Common.Interfaces](https://www.nuget.org/packages/Benday.Common.Interfaces). This means every entity can have file attachments stored in Azure Blob Storage via [Benday.BlobStorage](https://www.nuget.org/packages/Benday.BlobStorage) -- without adding any blob-specific code to the entity itself.

### How It Works

`TenantItemBase` provides a default `GetBlobPrefix()` that organizes blobs by tenant and entity ID:

```csharp
// TenantItemBase implements IBlobOwner with a default prefix
public abstract class TenantItemBase : CosmosIdentityBase, ITenantItem, IBlobOwner
{
    public virtual string GetBlobPrefix()
    {
        return $"{TenantId}/{Id}/";
    }
}
```

This means a `Note` entity with `TenantId = "acme"` and `Id = "note-123"` stores its blobs under the path `acme/note-123/` in your blob container. You can override `GetBlobPrefix()` in your entity class for a different convention.

### Setup

Add the Benday.BlobStorage and Benday.AzureStorage packages to your project:

```bash
dotnet add package Benday.AzureStorage --version 1.0.0-alpha
dotnet add package Benday.BlobStorage --version 1.0.0-alpha
```

Register Azure Storage services alongside your Cosmos DB services in `Program.cs`:

```csharp
using Benday.CosmosDb.Utilities;

var cosmosConfig = builder.Configuration.GetCosmosConfig();
var cosmosBuilder = new CosmosRegistrationHelper(builder.Services, cosmosConfig);

// Register Cosmos DB repositories
cosmosBuilder.RegisterRepositoryAndService<Note>();

// Register Azure Storage services and a blob container
builder.Services.AddBendayAzureStorage(builder.Configuration);
builder.Services.AddBlobRepository("attachments");
```

### Attaching Files to Cosmos DB Entities

Use `BlobBridge<T>` to manage file attachments for any entity that inherits from `TenantItemBase`:

```csharp
using Benday.BlobStorage;

public class NoteAttachmentService
{
    private readonly ITenantItemService<Note> _noteService;
    private readonly BlobBridge<Note> _blobBridge;

    public NoteAttachmentService(
        ITenantItemService<Note> noteService,
        IBlobRepository blobRepository)
    {
        _noteService = noteService;
        _blobBridge = new BlobBridge<Note>(blobRepository);
    }

    // Upload a file attachment to a note
    public async Task AttachFileAsync(string tenantId, string noteId, string filename, Stream content)
    {
        var note = await _noteService.GetByIdAsync(tenantId, noteId);

        // Stores the blob at "{TenantId}/{Id}/{filename}"
        // e.g., "acme/note-123/report.pdf"
        await _blobBridge.AttachAsync(note, filename, content);
    }

    // List all attachments for a note
    public async IAsyncEnumerable<string> ListAttachmentsAsync(Note note)
    {
        await foreach (var blob in _blobBridge.ListAttachmentsAsync(note))
        {
            yield return blob.Name;
        }
    }

    // Download an attachment
    public async Task<byte[]> DownloadAsync(Note note, string filename)
    {
        return await _blobBridge.DownloadAttachmentBytesAsync(note, filename);
    }

    // Get a time-limited SAS URL for downloading
    public Uri GetDownloadUrl(Note note, string filename)
    {
        return _blobBridge.GetAttachmentSasUri(
            note, filename,
            expiry: TimeSpan.FromMinutes(15),
            downloadFilename: filename);
    }

    // Delete all attachments when deleting a note
    public async Task DeleteNoteWithAttachmentsAsync(string tenantId, string noteId)
    {
        var note = await _noteService.GetByIdAsync(tenantId, noteId);

        // Delete blobs first, then the Cosmos DB document
        await _blobBridge.DeleteAttachmentsAsync(note);
        await _noteService.DeleteAsync(note);
    }
}
```

### Custom Blob Prefix

Override `GetBlobPrefix()` when you need a different blob path convention:

```csharp
public class Invoice : TenantItemBase
{
    public string InvoiceNumber { get; set; } = string.Empty;

    protected override string GetEntityTypeName() => nameof(Invoice);

    // Custom prefix: "acme/invoices/INV-2026-001/"
    public override string GetBlobPrefix()
    {
        return $"{TenantId}/invoices/{InvoiceNumber}/";
    }
}
```

### Parent-Child Entities with Attachments

Parented entities also implement `IBlobOwner` since `ParentedItemBase` extends `TenantItemBase`. This lets you attach files to child entities with blob paths that naturally include the tenant context:

```csharp
public class CommentAttachmentService
{
    private readonly IParentedItemService<Comment> _commentService;
    private readonly BlobBridge<Comment> _blobBridge;

    public CommentAttachmentService(
        IParentedItemService<Comment> commentService,
        IBlobRepository blobRepository)
    {
        _commentService = commentService;
        _blobBridge = new BlobBridge<Comment>(blobRepository);
    }

    public async Task AttachScreenshotAsync(Comment comment, Stream screenshot)
    {
        // Blob stored at "{TenantId}/{CommentId}/screenshot.png"
        await _blobBridge.AttachAsync(comment, "screenshot.png", screenshot);
    }
}
```

## Bulk Operations

v6 adds throttled bulk operations with configurable concurrency and retry logic for handling Cosmos DB rate limiting (HTTP 429):

```csharp
// Save many items with automatic throttling
await repository.SaveAllAsync(items);

// Customize concurrency and retries
await repository.SaveAllAsync(items, maxConcurrency: 10, maxRetries: 5);

// Delete all items for a tenant
await repository.DeleteAllByTenantIdAsync("acme");
```

## Paged Queries

Retrieve large result sets efficiently with continuation tokens:

```csharp
string? token = null;

do
{
    var page = await repository.GetPagedAsync("acme", pageSize: 50, continuationToken: token);

    foreach (var item in page.Items)
    {
        // Process each item
    }

    token = page.ContinuationToken;
} while (token != null);
```

## Sample Application

A complete working sample application is included in this repository demonstrating all major features:

- **Person Entity** - Demonstrates `TenantItemBase` with custom repository and service layer implementations. Shows complex domain models with nested Address objects.
- **Note Entity** - Simple `TenantItemBase` implementation using default repository and service registrations. Serves as parent entity for Comments.
- **Comment Entity** - Demonstrates `ParentedItemBase` pattern showing parent-child relationships with `ParentId` and `ParentEntityType` for type-safe queries.
- **LookupValue Entity** - Demonstrates custom repository configuration to store entities in a separate Cosmos DB container using the `RegisterRepository` overload with configuration options.

To run the sample application:
1. Start the Azure Cosmos DB Emulator (see [EMULATOR-SETUP.md](EMULATOR-SETUP.md))
2. Run `Benday.CosmosDb.SampleApp.WebUi`
3. Explore the different entity patterns and their implementations

## Upgrading from v5

See [MIGRATION-v6.md](MIGRATION-v6.md) for the complete migration guide. You can also use the migration guide as a prompt for AI coding agents (Claude Code, GitHub Copilot, etc.) to automate the code changes. Key changes:

- `OwnerId` renamed to `TenantId`, `DiscriminatorValue` renamed to `EntityType`
- `IOwnedItem` / `OwnedItemBase` renamed to `ITenantItem` / `TenantItemBase`
- Default partition key path changed from `/pk,/discriminator` to `/tenantId,/entityType`
- JSON serialization defaults to camelCase
- Use `cosmosmigrator migrate` to migrate existing container data

## Resources

[Full Documentation](https://benday-inc.github.io/Benday.CosmosDb/)  
[Sample Application](Benday.CosmosDb.SampleApp.WebUi) - Working examples of all patterns  
[NuGet Package](https://www.nuget.org/packages/Benday.CosmosDb/)  
[Source Code](https://github.com/benday-inc/Benday.CosmosDb)  
[Repository API Documentation](api/Benday.CosmosDb.Repositories.html)  
[Domain Model API Documentation](api/Benday.CosmosDb.DomainModels.html)  

## Feedback & Contributions

Got ideas for Cosmos DB functionality that you'd like to see? Found a bug? Let us know by [submitting an issue](https://github.com/benday-inc/Benday.CosmosDb/issues). Want to contribute? Submit a pull request.
