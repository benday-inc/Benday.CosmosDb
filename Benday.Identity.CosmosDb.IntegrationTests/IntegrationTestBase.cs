using Benday.Common.Testing;
using Benday.CosmosDb.Repositories;
using Benday.Identity.CosmosDb;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.IntegrationTests;

public abstract class IntegrationTestBase : TestClassBase
{
    protected CosmosEmulatorFixture Emulator { get; }

    protected IntegrationTestBase(
        CosmosEmulatorFixture emulator,
        ITestOutputHelper outputHelper) : base(outputHelper)
    {
        Emulator = emulator;
    }

    protected CosmosDbUserStore CreateUserStore()
    {
        var options = Options.Create(new CosmosRepositoryOptions<CosmosIdentityUser>
        {
            ConnectionString = Emulator.ConnectionString,
            DatabaseName = CosmosEmulatorFixture.DatabaseName,
            ContainerName = CosmosEmulatorFixture.UsersContainerName,
            PartitionKey = "/pk,/discriminator",
            UseHierarchicalPartitionKey = true,
            WithCreateStructures = false
        });

        var logger = new Mock<ILogger<CosmosDbUserStore>>();

        return new CosmosDbUserStore(options, Emulator.Client!, logger.Object);
    }

    protected CosmosDbRoleStore CreateRoleStore()
    {
        var options = Options.Create(new CosmosRepositoryOptions<CosmosIdentityRole>
        {
            ConnectionString = Emulator.ConnectionString,
            DatabaseName = CosmosEmulatorFixture.DatabaseName,
            ContainerName = CosmosEmulatorFixture.RolesContainerName,
            PartitionKey = "/pk,/discriminator",
            UseHierarchicalPartitionKey = true,
            WithCreateStructures = false
        });

        var logger = new Mock<ILogger<CosmosDbRoleStore>>();

        return new CosmosDbRoleStore(options, Emulator.Client!, logger.Object);
    }

    protected CosmosIdentityUser CreateTestUser(string? email = null)
    {
        email ??= $"test-{Guid.NewGuid():N}@example.com";
        return new CosmosIdentityUser
        {
            UserName = email,
            Email = email,
            PasswordHash = "hashed-password-value"
        };
    }

    protected CosmosIdentityRole CreateTestRole(string? name = null)
    {
        name ??= $"Role-{Guid.NewGuid():N}";
        return new CosmosIdentityRole { Name = name };
    }
}
