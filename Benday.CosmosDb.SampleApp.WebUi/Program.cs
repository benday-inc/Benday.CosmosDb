using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.SampleApp.Api.Repositories;
using Benday.CosmosDb.SampleApp.Api.ServiceLayers;
using Benday.CosmosDb.ServiceLayers;
using Benday.CosmosDb.Utilities;

var builder = WebApplication.CreateBuilder(args);

// add appsettings.json and appsettings.Development.json
builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
builder.Configuration.AddJsonFile("appsettings.unversioned.json", optional: true);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add CosmosDb support

var connectionString = 
    builder.Configuration.GetConnectionString("CosmosDb") ?? 
    throw new InvalidOperationException("Cannot find connection string 'CosmosDb'");

if (string.IsNullOrWhiteSpace(connectionString) == true)
{
    throw new InvalidOperationException($"Connection string is empty");
}

var databaseName =
    builder.Configuration["CosmosConfiguration:DatabaseName"] ?? throw new InvalidOperationException("Could not find database name");
var containerName =
    builder.Configuration["CosmosConfiguration:ContainerName"] ?? throw new InvalidOperationException("Could not find container name");
var partitionKey =
    builder.Configuration["CosmosConfiguration:PartitionKey"] ?? throw new InvalidOperationException("Could not find partition key");
var createStructures =
    builder.Configuration.GetValue<bool>("CosmosConfiguration:CreateStructures");

var cosmosBuilder = new CosmosConfigurationBuilder(
    builder.Services, connectionString, databaseName, containerName, createStructures, partitionKey);

cosmosBuilder.RegisterRepositoryAndService<Note>();
cosmosBuilder.RegisterRepository<Person, IPersonRepository, CosmosDbPersonRepository>();
builder.Services.AddTransient<IPersonService, PersonService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Person}/{action=Index}/{id?}");

app.Run();
