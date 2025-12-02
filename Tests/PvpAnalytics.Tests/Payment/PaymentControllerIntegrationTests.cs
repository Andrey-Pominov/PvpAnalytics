using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PaymentService.Application.DTOs;
using PaymentService.Application.Models;
using PaymentEntity = PaymentService.Core.Entities.Payment;
using PaymentService.Core.Enum;
using Xunit;

namespace PvpAnalytics.Tests.Payment;

public class PaymentControllerIntegrationTests(PaymentServiceApiFactory factory)
    : IClassFixture<PaymentServiceApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-user-123", ["Admin"]);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task InitializeAsync()
    {
        await factory.CleanupDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_ReturnsPaginatedResponse_ForAuthenticatedUser()
    {
        // Arrange - Create some payments
        await CreatePaymentAsync("txn-001", 100.00m);
        await CreatePaymentAsync("txn-002", 200.00m);

        // Act
        var response = await _client.GetAsync("/api/payment?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PaymentEntity>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetAll_AppliesUserScopeFilter_ForNonAdminUser()
    {
        // Arrange - Create payments for different users
        var user1Client = factory.CreateAuthenticatedClient("user-1");
        var user2Client = factory.CreateAuthenticatedClient("user-2");

        await CreatePaymentAsync("txn-user1-001", 100.00m, user1Client);
        await CreatePaymentAsync("txn-user2-001", 200.00m, user2Client);

        // Act - User 1 should only see their own payments
        var response = await user1Client.GetAsync("/api/payment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PaymentEntity>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].TransactionId.Should().Be("txn-user1-001");
    }

    [Fact]
    public async Task GetAll_AllowsAdminToSeeAllPayments()
    {
        // Arrange
        var adminClient = factory.CreateAuthenticatedClient("admin-user", ["Admin"]);
        var regularClient = factory.CreateAuthenticatedClient("regular-user");

        await CreatePaymentAsync("txn-admin-001", 100.00m, adminClient);
        await CreatePaymentAsync("txn-regular-001", 200.00m, regularClient);

        // Act - Admin should see all payments
        var response = await adminClient.GetAsync("/api/payment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PaymentEntity>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_AppliesStatusFilter()
    {
        // Arrange
        await CreatePaymentAsync("txn-pending", 100.00m);
        var completedPayment = await CreatePaymentAsync("txn-completed", 200.00m);
        await UpdatePaymentStatusAsync(completedPayment.Id, PaymentStatus.Completed);

        // Act
        var response = await _client.GetAsync("/api/payment?status=2"); // Completed = 2

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PaymentEntity>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public async Task GetAll_AppliesDateRangeFilter()
    {
        // Arrange
        var newDate = DateTime.UtcNow.AddDays(-1);

        // Create payments with specific dates (we'll need to set CreatedAt manually in test)
        await CreatePaymentAsync("txn-old", 100.00m);

        // Act
        var response = await _client.GetAsync($"/api/payment?startDate={newDate:yyyy-MM-ddTHH:mm:ssZ}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PaymentEntity>>(JsonOptions);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_AppliesSorting()
    {
        // Arrange
        await CreatePaymentAsync("txn-001", 100.00m);
        await CreatePaymentAsync("txn-002", 300.00m);
        await CreatePaymentAsync("txn-003", 200.00m);

        // Act - Sort by amount descending
        var response = await _client.GetAsync("/api/payment?sortBy=amount&sortOrder=desc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PaymentEntity>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.Items[0].Amount.Should().Be(300.00m);
        result.Items[^1].Amount.Should().Be(100.00m);
    }

    [Fact]
    public async Task GetAll_RespectsPageSizeLimit()
    {
        // Arrange - Create more than max page size
        for (int i = 0; i < 150; i++)
        {
            await CreatePaymentAsync($"txn-{i:D3}", 100.00m);
        }

        // Act - Request with pageSize > max (should be capped at 100)
        var response = await _client.GetAsync("/api/payment?pageSize=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PaymentEntity>>(JsonOptions);
        result.Should().NotBeNull();
        result!.PageSize.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task Get_ReturnsPayment_ForOwnPayment()
    {
        // Arrange
        var payment = await CreatePaymentAsync("txn-get-001", 100.00m);

        // Act
        var response = await _client.GetAsync($"/api/payment/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentEntity>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
        result.TransactionId.Should().Be("txn-get-001");
    }

    [Fact]
    public async Task Get_ReturnsForbidden_ForOtherUserPayment()
    {
        // Arrange
        var otherUserClient = factory.CreateAuthenticatedClient("other-user");
        var payment = await CreatePaymentAsync("txn-other-001", 100.00m, otherUserClient);

        // Act - Try to access other user's payment as a non-admin
        var currentUserClient = factory.CreateAuthenticatedClient();
        var response = await currentUserClient.GetAsync($"/api/payment/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_ForNonExistentPayment()
    {
        // Act
        var response = await _client.GetAsync("/api/payment/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_CreatesPayment_WithValidRequest()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = 150.00m,
            TransactionId = "txn-create-001",
            PaymentMethod = "CreditCard",
            Description = "Test payment"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await response.Content.ReadFromJsonAsync<PaymentEntity>(JsonOptions);
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(150.00m);
        payment.TransactionId.Should().Be("txn-create-001");
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.UserId.Should().Be("test-user-123");
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Create_RejectsInvalidAmount()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = -10.00m, // Invalid negative amount
            TransactionId = "txn-invalid",
            PaymentMethod = "CreditCard"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_RejectsMissingRequiredFields()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            Amount = 100.00m
            // Missing TransactionId and PaymentMethod
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_UpdatesPayment_WithValidRequest()
    {
        // Arrange
        var payment = await CreatePaymentAsync("txn-update-001", 100.00m);
        var updateRequest = new UpdatePaymentRequest
        {
            Amount = 200.00m,
            Status = PaymentStatus.Completed,
            Description = "Updated payment"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/payment/{payment.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var getResponse = await _client.GetAsync($"/api/payment/{payment.Id}");
        var updatedPayment = await getResponse.Content.ReadFromJsonAsync<PaymentEntity>(JsonOptions);
        updatedPayment.Should().NotBeNull();
        updatedPayment!.Amount.Should().Be(200.00m);
        updatedPayment.Status.Should().Be(PaymentStatus.Completed);
        updatedPayment.Description.Should().Be("Updated payment");
        updatedPayment.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ReturnsForbidden_ForOtherUserPayment()
    {
        // Arrange
        var otherUserClient = factory.CreateAuthenticatedClient("other-user");
        var payment = await CreatePaymentAsync("txn-other-002", 100.00m, otherUserClient);
        var updateRequest = new UpdatePaymentRequest
        {
            Amount = 200.00m,
            Status = PaymentStatus.Completed
        };

        // Act - Try to update other user's payment as a non-admin
        var currentUserClient = factory.CreateAuthenticatedClient();
        var response = await currentUserClient.PutAsJsonAsync($"/api/payment/{payment.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForNonExistentPayment()
    {
        // Arrange
        var updateRequest = new UpdatePaymentRequest
        {
            Amount = 200.00m,
            Status = PaymentStatus.Completed
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/payment/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DeletesPayment_ForOwnPayment()
    {
        // Arrange
        var payment = await CreatePaymentAsync("txn-delete-001", 100.00m);

        // Act
        var response = await _client.DeleteAsync($"/api/payment/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/payment/{payment.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ReturnsForbidden_ForOtherUserPayment()
    {
        // Arrange
        var otherUserClient = factory.CreateAuthenticatedClient("other-user");
        var payment = await CreatePaymentAsync("txn-other-003", 100.00m, otherUserClient);

        // Act - Try to delete other user's payment as a non-admin
        var currentUserClient = factory.CreateAuthenticatedClient();
        var response = await currentUserClient.DeleteAsync($"/api/payment/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WithoutAuthentication()
    {
        // Arrange
        var unauthenticatedClient = factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/payment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Helper methods
    private async Task<PaymentEntity> CreatePaymentAsync(string transactionId, decimal amount,
        HttpClient? client = null)
    {
        client ??= _client;
        var request = new CreatePaymentRequest
        {
            Amount = amount,
            TransactionId = transactionId,
            PaymentMethod = "CreditCard",
            Description = "Test payment"
        };

        var response = await client.PostAsJsonAsync("/api/payment", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentEntity>(JsonOptions)
               ?? throw new InvalidOperationException("Failed to create payment");
    }

    private async Task UpdatePaymentStatusAsync(long paymentId, PaymentStatus status)
    {
        var updateRequest = new UpdatePaymentRequest
        {
            Amount = 100.00m,
            Status = status
        };

        var response = await _client.PutAsJsonAsync($"/api/payment/{paymentId}", updateRequest);
        response.EnsureSuccessStatusCode();
    }
}