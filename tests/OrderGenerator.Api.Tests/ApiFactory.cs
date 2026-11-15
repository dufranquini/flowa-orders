using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OrderGenerator.Api.Fix;

namespace OrderGenerator.Api.Tests;


public class ApiFactory : WebApplicationFactory<Program>
{
    public FakeOrderGateway Gateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>(); // remove FixInitiatorService
            services.RemoveAll<IOrderGateway>();
            services.AddSingleton<IOrderGateway>(Gateway);
        });
    }
}
