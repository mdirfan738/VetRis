using System.Net;
using FluentAssertions;
using Xunit;

namespace VetRIS.Api.IntegrationTests;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_IsAnonymous_NoTokenRequired()
    {
        // Deliberately not attaching an Authorization header — /health must
        // stay reachable for load balancer health checks regardless of auth.
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
