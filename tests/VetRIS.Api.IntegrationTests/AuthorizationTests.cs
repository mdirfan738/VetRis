using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace VetRIS.Api.IntegrationTests;

public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStudies_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/studies");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInvoices_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorizedNotServerError()
    {
        // Also exercises the global exception handler indirectly: a bad
        // login must produce a clean 401, not a 500 from something
        // upstream throwing on a null user.
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong-password" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_DoesNotRequireAuthorization()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
