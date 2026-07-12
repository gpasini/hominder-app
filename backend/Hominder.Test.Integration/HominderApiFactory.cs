using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Hominder.Test.Integration;

public sealed class HominderApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Hominder", _database.GetConnectionString());

    async Task IAsyncLifetime.InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _database.DisposeAsync();
}
