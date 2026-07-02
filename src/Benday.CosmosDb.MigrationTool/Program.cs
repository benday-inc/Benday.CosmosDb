using Benday.CommandsFramework;
using Benday.CosmosDb.MigrationTool;

CommandsApp
    .Create<MigrateContainerCommand>(args)
    .WithAppInfo("Benday.CosmosDb.MigrationTool",
        "https://github.com/benday-inc/Benday.CosmosDb")
    .WithVersionFromAssembly()
    .Run();
