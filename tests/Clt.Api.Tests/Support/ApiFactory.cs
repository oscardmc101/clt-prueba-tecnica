using Clt.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clt.Api.Tests.Support;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "clt-tests", Guid.NewGuid().ToString("N"));
    private readonly IInterceptor[] _interceptors;

    public ApiFactory() : this([]) { }
    internal ApiFactory(params IInterceptor[] interceptors) => _interceptors = interceptors;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_directory);
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContextFactory<AppDbContext>(options => options
                .UseSqlite($"Data Source={Path.Combine(_directory, "test.db")};Foreign Keys=True;Pooling=False;Default Timeout=10")
                .AddInterceptors(_interceptors));
        });
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", "clt-local-test-key");
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
