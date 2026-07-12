namespace Hominder.Test.Integration;

public class ApiSmokeTests : IClassFixture<HominderApiFactory>
{
    private readonly HominderApiFactory _factory;

    public ApiSmokeTests(HominderApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
