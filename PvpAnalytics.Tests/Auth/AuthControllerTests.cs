using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthService.Application.DTOs;
using AuthService.Application.Models;
using FluentAssertions;
using Xunit;

namespace PvpAnalytics.Tests.Auth;

public class AuthControllerTests : IClassFixture<AuthServiceApiFactory>
{
    private readonly AuthServiceApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthControllerTests(AuthServiceApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsTokens_ForNewUser()
    {
        var request = new RegisterRequest("newuser@example.com", "StrongP@ss1!", "New User");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"body: {body}");
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrEmpty();
        payload.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_ForDuplicateEmail()
    {
        var request = new RegisterRequest("duplicate@example.com", "StrongP@ss1!", "Duplicate User");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"body: {body}");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.TryGetProperty("error", out var error).Should().BeTrue();
        error.GetString().Should().Be("User already exists.");
    }

    [Fact]
    public async Task Login_ReturnsTokens_ForValidCredentials()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        var password = "StrongP@ss1!";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Login User"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"body: {body}");
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_ForInvalidCredentials()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("missing@example.com", "Wrong!23"));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"body: {body}");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.TryGetProperty("error", out var error).Should().BeTrue();
        error.GetString().Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Refresh_ReturnsNewTokens_ForValidRefreshToken()
    {
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        var password = "StrongP@ss1!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Refresh User"));
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        registerPayload.Should().NotBeNull();

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(registerPayload!.RefreshToken));
        var body = await refreshResponse.Content.ReadAsStringAsync();

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"body: {body}");
        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        refreshPayload.Should().NotBeNull();
        refreshPayload!.AccessToken.Should().NotBe(registerPayload.AccessToken);
    }
}

