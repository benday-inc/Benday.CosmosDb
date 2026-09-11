using Benday.CommandsFramework;

namespace Benday.CosmosDb.MigrationTool.Tests;

/// <summary>
/// Turns the framework's own registration checks into part of the suite, so a
/// command name that collides with a reserved keyword (tui, completion, ...) or
/// an argument definition the framework rejects fails here rather than at run
/// time. Both checks were available in v4 and called by nothing.
/// </summary>
public class CommandRegistryTests
{
    /// <summary>
    /// Mirrors what Program.cs configures, so the registry under test is the one
    /// the tool actually builds at run time. UsesConfiguration in particular
    /// changes which commands are registered.
    /// </summary>
    private static DefaultProgramOptions GetProgramOptions()
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Benday.CosmosDb.MigrationTool",
            Website = "https://github.com/benday-inc/Benday.CosmosDb",
            UsesConfiguration = true,
            StrictArgumentValidation = true
        };
    }

    [Fact]
    public void CommandsHaveNoRegistryProblems()
    {
        var registry = CommandRegistry.Build(
            GetProgramOptions(), typeof(MigrateContainerCommand).Assembly);

        // print them -- "Assert.Empty failed, collection had 3 items" does not say which
        foreach (var problem in registry.Problems)
        {
            Console.WriteLine(problem);
        }

        Assert.Empty(registry.Problems);
    }

    [Fact]
    public void CommandsHaveNoArgumentProblems()
    {
        var utility = new CommandAttributeUtility(GetProgramOptions());

        var problems = utility.GetArgumentProblems(typeof(MigrateContainerCommand).Assembly);

        foreach (var problem in problems)
        {
            Console.WriteLine(problem);
        }

        Assert.Empty(problems);
    }
}
