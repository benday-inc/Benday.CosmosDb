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

var cosmosConfig = builder.Configuration.GetCosmosConfig();

var helper = new CosmosRegistrationHelper(
    builder.Services, cosmosConfig);

helper.RegisterRepositoryAndService<Note>();
helper.RegisterParentedRepositoryAndService<Comment>();
helper.RegisterRepository<Person, IPersonRepository, CosmosDbPersonRepository>();
builder.Services.AddTransient<IPersonService, PersonService>();

helper.RegisterRepository<LookupValue, ILookupValueRepository, CosmosDbLookupValueRepository>(
    containerName: "LookupValues", withCreateStructures: true
);

builder.Services.AddTransient<ILookupValueService, LookupValueService>();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
