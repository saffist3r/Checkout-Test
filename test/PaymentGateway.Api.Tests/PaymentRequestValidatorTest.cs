using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Tests.Fakes;

namespace PaymentGateway.Api.Tests;

public class PaymentRequestValidatorTest
{
    private readonly PaymentRequestValidator _validator = new(new FixedTimeProvider(TestData.Now));

    [Fact]
    public void ShouldAcceptValidRequest()
    {
        _validator.Validate(TestData.ValidRequest()).ShouldBeEmpty();
    }

    [Fact]
    public void ShouldReportEveryMissingField()
    {
        Dictionary<string, string[]> errors = _validator.Validate(new PostPaymentRequest());

        errors.Keys.Order().ShouldBe(new[] { "amount", "cardNumber", "currency", "cvv", "expiryMonth", "expiryYear" });
    }

    [Theory]
    [InlineData("12345678901234")]
    [InlineData("1234567890123456789")]
    [InlineData("0000000000000001")]
    public void ShouldAcceptCardNumberOfValidLength(string cardNumber)
    {
        _validator.Validate(TestData.ValidRequest(cardNumber)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("1234567890123")]
    [InlineData("12345678901234567890")]
    [InlineData("1234 5678 9012 3456")]
    [InlineData("12345678901234ab")]
    public void ShouldRejectInvalidCardNumber(string cardNumber)
    {
        _validator.Validate(TestData.ValidRequest(cardNumber)).Keys.ShouldContain("cardNumber");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void ShouldAcceptExpiryMonthBoundaries(int month)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.ExpiryMonth = month;

        _validator.Validate(request).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void ShouldRejectOutOfRangeExpiryMonth(int month)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.ExpiryMonth = month;

        _validator.Validate(request).Keys.ShouldContain("expiryMonth");
    }

    [Theory]
    [InlineData(9, 2026)]
    [InlineData(12, 2025)]
    public void ShouldRejectExpiredCard(int month, int year)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.ExpiryMonth = month;
        request.ExpiryYear = year;

        _validator.Validate(request).Keys.ShouldContain("expiryYear");
    }

    [Theory]
    [InlineData(10, 2026)]
    [InlineData(11, 2026)]
    [InlineData(1, 2027)]
    public void ShouldAcceptCardExpiringThisMonthOrLater(int month, int year)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.ExpiryMonth = month;
        request.ExpiryYear = year;

        _validator.Validate(request).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void ShouldAcceptSupportedCurrency(string currency)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.Currency = currency;

        _validator.Validate(request).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("gbp")]
    [InlineData("GB")]
    [InlineData("GBPP")]
    public void ShouldRejectUnsupportedCurrency(string currency)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.Currency = currency;

        _validator.Validate(request).Keys.ShouldContain("currency");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void ShouldRejectNonPositiveAmount(int amount)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.Amount = amount;

        _validator.Validate(request).Keys.ShouldContain("amount");
    }

    [Theory]
    [InlineData("012")]
    [InlineData("1234")]
    public void ShouldAcceptValidCvv(string cvv)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.Cvv = cvv;

        _validator.Validate(request).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12a")]
    public void ShouldRejectInvalidCvv(string cvv)
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.Cvv = cvv;

        _validator.Validate(request).Keys.ShouldContain("cvv");
    }
}
