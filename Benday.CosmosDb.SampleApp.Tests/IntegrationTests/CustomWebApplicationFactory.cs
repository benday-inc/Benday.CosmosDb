using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.CosmosDb.SampleApp.Tests.IntegrationTests;

public class CustomWebApplicationFactory<TStartup> :
    WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            
        });

        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TStartup).Assembly);
        });

        builder.Configure(app =>
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });  
        });
    }

    protected IServiceScope? _scope;
    protected IServiceScope Scope
    {
        get
        {
            if (_scope == null)
            {
                // EnsureClientIsInitialized();

                var scopeFactory = Services.GetService(
                    typeof(IServiceScopeFactory)) as IServiceScopeFactory;

                if (scopeFactory == null)
                {
                    throw new InvalidOperationException(
                        "Could not get instance of IServiceScopeFactory.");
                }

                _scope = scopeFactory.CreateScope();
            }

            return _scope;
        }
    }

    public T? CreateInstance<T>()
    {
        var provider = Scope.ServiceProvider;

        var returnValue = provider.GetService<T>();

        return returnValue;
    }
}
