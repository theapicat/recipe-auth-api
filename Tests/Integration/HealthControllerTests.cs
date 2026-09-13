using System.Net;
using Shouldly;
using Tests.Support;

namespace Tests.Integration;

public class HealthControllerTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly AuthApiWebApplicationFactory _factory;

    public HealthControllerTests(AuthApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_Health_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
