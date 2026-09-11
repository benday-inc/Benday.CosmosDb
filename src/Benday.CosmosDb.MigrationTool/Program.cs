using Benday.CommandsFramework;
using Benday.CosmosDb.MigrationTool;

// The exit code has to be returned: nothing in the framework assigns
// Environment.ExitCode any more, so a discarded result always exits 0.
return await CommandsApp
    .Create<MigrateContainerCommand>(args)
    .WithAppInfo("Benday.CosmosDb.MigrationTool",
        "https://github.com/benday-inc/Benday.CosmosDb")
    .WithVersionFromAssembly()
    // A typo'd --nmae value would otherwise swallow the next token silently under the POSIX
    // syntax; strict validation turns an unknown argument into a validation failure.
    .ConfigureOptions(o => o.StrictArgumentValidation = true)
    .RunAsync();
