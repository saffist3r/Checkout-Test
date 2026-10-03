using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Services.Bank;
using PaymentGateway.Api.Tests.Fakes;

namespace PaymentGateway.Api.Tests;

public class PaymentsServiceTest
{
    private readonly FakeAcquiringBankClient _bank = new();
    private readonly PaymentsRepository _repository = new();
    private readonly PaymentsService _service;

    public PaymentsServiceTest()
    {
        _service = new PaymentsService(_bank, _repository);
    }

    [Fact]
    public async Task ShouldStoreAuthorizedPayment()
    {
        Payment payment = await _service.ProcessAsync(TestData.ValidRequest(TestData.AuthorizedCard));

        payment.Status.ShouldBe(PaymentStatus.Authorized);
        payment.AuthorizationCode.ShouldNotBeNullOrEmpty();
        _repository.Get(payment.Id).ShouldBe(payment);
    }

    [Fact]
    public async Task ShouldStoreDeclinedPayment()
    {
        Payment payment = await _service.ProcessAsync(TestData.ValidRequest(TestData.DeclinedCard));

        payment.Status.ShouldBe(PaymentStatus.Declined);
        _repository.Get(payment.Id).ShouldBe(payment);
    }

    [Fact]
    public async Task ShouldKeepOnlyLastFourDigits()
    {
        Payment payment = await _service.ProcessAsync(TestData.ValidRequest("1234567890120877"));

        payment.CardNumberLastFour.ShouldBe("0877");
    }

    [Fact]
    public async Task ShouldSendExpiryDateAsZeroPaddedMonthAndYear()
    {
        PostPaymentRequest request = TestData.ValidRequest();
        request.ExpiryMonth = 4;
        request.ExpiryYear = 2027;

        await _service.ProcessAsync(request);

        BankPaymentRequest sent = _bank.Requests.ShouldHaveSingleItem();
        sent.ExpiryDate.ShouldBe("04/2027");
        sent.CardNumber.ShouldBe(request.CardNumber);
        sent.Cvv.ShouldBe(request.Cvv);
        sent.Currency.ShouldBe(request.Currency);
        sent.Amount.ShouldBe(request.Amount!.Value);
    }

    [Fact]
    public async Task ShouldPropagateBankUnavailable()
    {
        await Should.ThrowAsync<BankUnavailableException>(
            () => _service.ProcessAsync(TestData.ValidRequest(TestData.BankUnavailableCard)));
    }
}
