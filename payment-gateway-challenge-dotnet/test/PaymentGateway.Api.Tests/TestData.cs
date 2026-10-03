using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Tests;

public static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    public const string AuthorizedCard = "2222405343248877";
    public const string DeclinedCard = "2222405343248112";
    public const string BankUnavailableCard = "2222405343248110";

    public static PostPaymentRequest ValidRequest(string cardNumber = AuthorizedCard) => new()
    {
        CardNumber = cardNumber,
        ExpiryMonth = 4,
        ExpiryYear = Now.Year + 1,
        Currency = "GBP",
        Amount = 1050,
        Cvv = "123"
    };
}
