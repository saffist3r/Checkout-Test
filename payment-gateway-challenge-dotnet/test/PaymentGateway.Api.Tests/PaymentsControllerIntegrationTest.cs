using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Services.Bank;
using PaymentGateway.Api.Tests.Fakes;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string PaymentsPath = "/api/payments";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly FakeAcquiringBankClient _bank = new();
    private readonly PaymentsRepository _repository = new();
    private readonly HttpClient _client;

    public PaymentsControllerIntegrationTest(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAcquiringBankClient>();
            services.AddSingleton<IAcquiringBankClient>(_bank);
            services.RemoveAll<PaymentsRepository>();
            services.AddSingleton(_repository);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(TestData.Now));
        })).CreateClient();
    }

    [Theory]
    [InlineData(TestData.AuthorizedCard, PaymentStatus.Authorized)]
    [InlineData(TestData.DeclinedCard, PaymentStatus.Declined)]
    public async Task ShouldProcessPayment(string cardNumber, PaymentStatus expectedStatus)
    {
        PostPaymentRequest request = TestData.ValidRequest(cardNumber);

        HttpResponseMessage response = await _client.PostAsJsonAsync(PaymentsPath, request);
        PaymentResponse? payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payment.ShouldNotBeNull();
        payment.Id.ShouldNotBe(Guid.Empty);
        payment.Status.ShouldBe(expectedStatus);
        payment.CardNumberLastFour.ShouldBe(cardNumber[^4..]);
        payment.ExpiryMonth.ShouldBe(request.ExpiryMonth!.Value);
        payment.ExpiryYear.ShouldBe(request.ExpiryYear!.Value);
        payment.Currency.ShouldBe(request.Currency);
        payment.Amount.ShouldBe(request.Amount!.Value);
    }

    [Fact]
    public async Task ShouldNeverReturnFullCardNumberOrCvv()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(PaymentsPath, TestData.ValidRequest());
        string body = await response.Content.ReadAsStringAsync();

        body.ShouldNotContain(TestData.AuthorizedCard);
        body.ShouldNotContain("cvv", Case.Insensitive);
    }

    [Fact]
    public async Task ShouldReturnStatusAsString()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(PaymentsPath, TestData.ValidRequest());
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        body.RootElement.GetProperty("status").GetString().ShouldBe("Authorized");
    }

    [Fact]
    public async Task ShouldRejectInvalidPaymentWithoutCallingBank()
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.CardNumber = "123";
        request.Currency = "JPY";

        HttpResponseMessage response = await _client.PostAsJsonAsync(PaymentsPath, request);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        body.RootElement.GetProperty("status").GetString().ShouldBe("Rejected");
        JsonElement errors = body.RootElement.GetProperty("errors");
        errors.TryGetProperty("cardNumber", out _).ShouldBeTrue();
        errors.TryGetProperty("currency", out _).ShouldBeTrue();
        _bank.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShouldRejectMalformedBodyInSameShape()
    {
        StringContent content = new(
            """{"cardNumber":"2222405343248877","expiryMonth":4,"expiryYear":2027,"currency":"GBP","amount":10.5,"cvv":"123"}""",
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await _client.PostAsync(PaymentsPath, content);
        string raw = await response.Content.ReadAsStringAsync();
        using JsonDocument body = JsonDocument.Parse(raw);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        body.RootElement.GetProperty("status").GetString().ShouldBe("Rejected");
        body.RootElement.GetProperty("errors").TryGetProperty("amount", out _).ShouldBeTrue();
        raw.ShouldNotContain(TestData.AuthorizedCard);
        _bank.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShouldReturnBadGatewayWhenBankIsUnavailable()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(PaymentsPath, TestData.ValidRequest(TestData.BankUnavailableCard));

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task ShouldRetrieveProcessedPayment()
    {
        HttpResponseMessage postResponse = await _client.PostAsJsonAsync(PaymentsPath, TestData.ValidRequest());
        PaymentResponse? created = await postResponse.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        HttpResponseMessage response = await _client.GetAsync($"{PaymentsPath}/{created!.Id}");
        PaymentResponse? retrieved = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        retrieved.ShouldBe(created);
    }

    [Fact]
    public async Task ShouldRetrievePaymentFromRepository()
    {
        Payment payment = new(Guid.NewGuid(), PaymentStatus.Declined, "0123", 12, 2030, "USD", 999, null);
        _repository.Add(payment);

        PaymentResponse? retrieved = await _client.GetFromJsonAsync<PaymentResponse>($"{PaymentsPath}/{payment.Id}", JsonOptions);

        retrieved.ShouldBe(PaymentResponse.From(payment));
    }

    [Fact]
    public async Task ShouldReturnNotFoundForUnknownPayment()
    {
        HttpResponseMessage response = await _client.GetAsync($"{PaymentsPath}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
