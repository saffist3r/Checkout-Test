using System.Net;
using System.Text;
using System.Text.Json;

using PaymentGateway.Api.Services.Bank;
using PaymentGateway.Api.Tests.Fakes;

namespace PaymentGateway.Api.Tests;

public class AcquiringBankClientTest
{
    private static readonly BankPaymentRequest Request = new("2222405343248877", "04/2027", "GBP", 100, "123");

    private static AcquiringBankClient CreateClient(StubHttpMessageHandler handler)
    {
        return new AcquiringBankClient(new HttpClient(handler) { BaseAddress = new Uri("http://bank.test/") });
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
    {
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }

    [Fact]
    public async Task ShouldPostBankContractInSnakeCase()
    {
        StubHttpMessageHandler handler = new(_ => Json(HttpStatusCode.OK, """{"authorized":true,"authorization_code":"abc"}"""));

        await CreateClient(handler).ProcessPaymentAsync(Request);

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().ShouldBe("http://bank.test/payments");
        using JsonDocument body = JsonDocument.Parse(handler.LastRequestBody!);
        body.RootElement.GetProperty("card_number").GetString().ShouldBe("2222405343248877");
        body.RootElement.GetProperty("expiry_date").GetString().ShouldBe("04/2027");
        body.RootElement.GetProperty("currency").GetString().ShouldBe("GBP");
        body.RootElement.GetProperty("amount").GetInt32().ShouldBe(100);
        body.RootElement.GetProperty("cvv").GetString().ShouldBe("123");
    }

    [Theory]
    [InlineData("""{"authorized":true,"authorization_code":"abc"}""", true, "abc")]
    [InlineData("""{"authorized":false,"authorization_code":""}""", false, "")]
    public async Task ShouldReadBankDecision(string responseBody, bool authorized, string code)
    {
        StubHttpMessageHandler handler = new(_ => Json(HttpStatusCode.OK, responseBody));

        BankPaymentResponse response = await CreateClient(handler).ProcessPaymentAsync(Request);

        response.Authorized.ShouldBe(authorized);
        response.AuthorizationCode.ShouldBe(code);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ShouldThrowBankUnavailableOnErrorStatus(HttpStatusCode status)
    {
        StubHttpMessageHandler handler = new(_ => Json(status, "{}"));

        BankUnavailableException exception = await Should.ThrowAsync<BankUnavailableException>(
            () => CreateClient(handler).ProcessPaymentAsync(Request));

        exception.HttpStatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task ShouldThrowBankUnavailableWhenBankCannotBeReached()
    {
        StubHttpMessageHandler handler = new(_ => throw new HttpRequestException("connection refused"));

        BankUnavailableException exception = await Should.ThrowAsync<BankUnavailableException>(
            () => CreateClient(handler).ProcessPaymentAsync(Request));

        exception.HttpStatusCode.ShouldBeNull();
    }

    [Fact]
    public async Task ShouldThrowBankUnavailableOnUnreadableResponse()
    {
        StubHttpMessageHandler handler = new(_ => Json(HttpStatusCode.OK, "not json"));

        await Should.ThrowAsync<BankUnavailableException>(() => CreateClient(handler).ProcessPaymentAsync(Request));
    }
}
