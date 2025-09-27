# Azure Cosmos DB Linux Emulator Setup

This guide shows how to configure Benday.CosmosDb for use with the Azure Cosmos DB Linux emulator.

## Quick Setup (Recommended)

### Option 1: Using appsettings.json (Ultra Simple)

Just set `UseEmulator: true` and everything else gets smart defaults:

```json
{
  "CosmosConfiguration": {
    "UseEmulator": true
  }
}
```

That's it! When `UseEmulator: true` is set, the library automatically configures:
- 🗄️ **DatabaseName**: `"DevDb"`
- 📦 **ContainerName**: `"DevContainer"`
- 🔑 **PartitionKey**: `"/pk,/discriminator"`
- 🌳 **HierarchicalPartitionKey**: `true`
- ⚡ **DatabaseThroughput**: `400 RU/s`
- ✅ **Endpoint**: `https://localhost:8081/` 
- ✅ **AccountKey**: Standard emulator key
- ✅ **GatewayMode**: `true` (required for Linux emulator)
- ✅ **AllowBulkExecution**: `false` (not supported in emulator)
- ✅ **CreateStructures**: `true` (convenient for development)

### Option 2: Using CosmosConfigBuilder in Code

```csharp
var config = new CosmosConfigBuilder()
    .ForEmulator()
    .WithDatabase("MyTestDb")
    .WithContainer("MyContainer")
    .Build();

var cosmosHelper = new CosmosRegistrationHelper(services, config);
```

## Linux Emulator Limitations

The Linux emulator has specific requirements that this library handles automatically:

| Feature | Linux Emulator | Library Default | Auto-Applied |
|---------|----------------|-----------------|--------------|
| Connection Mode | **Gateway Only** | Direct | ✅ Set to Gateway |
| Bulk Execution | **Not Supported** | Enabled | ✅ Disabled |
| Hierarchical Partition Keys | Limited Support | Disabled | ✅ Stays Disabled |

## Development vs Production

### Development (appsettings.json)
```json
{
  "CosmosConfiguration": {
    "UseEmulator": true
  }
}
```

Or with custom names:
```json
{
  "CosmosConfiguration": {
    "UseEmulator": true,
    "DatabaseName": "MyTestDb",
    "ContainerName": "MyTestContainer"
  }
}
```

### Production (appsettings.Production.json)
```json
{
  "CosmosConfiguration": {
    "UseEmulator": false,
    "UseDefaultAzureCredential": true,
    "Endpoint": "https://your-cosmos.documents.azure.com:443/",
    "DatabaseName": "ProductionDb",
    "ContainerName": "ProductionContainer",
    "GatewayMode": false,
    "AllowBulkExecution": true,
    "CreateStructures": false
  }
}
```

## Manual Configuration (Advanced)

If you need full control, you can still configure everything manually:

```json
{
  "CosmosConfiguration": {
    "UseEmulator": false,
    "Endpoint": "https://localhost:8081/",
    "AccountKey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "TestDb",
    "ContainerName": "TestContainer",
    "GatewayMode": true,
    "AllowBulkExecution": false,
    "CreateStructures": true
  }
}
```

## Docker Compose Example

Here's a complete setup with the Linux emulator:

```yaml
version: '3.8'
services:
  cosmos-emulator:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    ports:
      - "8081:8081"
      - "10250-10255:10250-10255"
    environment:
      - AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10
      - AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true
    volumes:
      - cosmos-data:/data

  your-app:
    build: .
    depends_on:
      - cosmos-emulator
    environment:
      - CosmosConfiguration__UseEmulator=true
      - CosmosConfiguration__DatabaseName=MyApp
      - CosmosConfiguration__ContainerName=Items

volumes:
  cosmos-data:
```

## Troubleshooting

### Common Issues

1. **Connection Refused (localhost:8081)**
   - Make sure the emulator is running
   - Check that port 8081 is accessible

2. **Bulk Operation Errors** 
   - Set `"UseEmulator": true` or manually set `"AllowBulkExecution": false`

3. **Direct Mode Errors**
   - Set `"UseEmulator": true` or manually set `"GatewayMode": true`

### Verification

Test your configuration works:

```csharp
// This should work without errors when emulator is running
var config = builder.Configuration.GetCosmosConfig();
var cosmosBuilder = new CosmosRegistrationHelper(services, config);
```

## Migration from Manual Configuration

### Before (Manual)
```json
{
  "CosmosConfiguration": {
    "Endpoint": "https://localhost:8081/",
    "AccountKey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "TestDb",
    "ContainerName": "TestContainer",
    "GatewayMode": true,
    "AllowBulkExecution": false,
    "CreateStructures": true
  }
}
```

### After (Simplified)
```json
{
  "CosmosConfiguration": {
    "UseEmulator": true,
    "DatabaseName": "TestDb",
    "ContainerName": "TestContainer"
  }
}
```

Much cleaner and less error-prone!