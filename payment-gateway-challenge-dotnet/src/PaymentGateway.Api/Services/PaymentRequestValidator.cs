using System.Text.Json;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services;

public class PaymentRequestValidator
{
    private const int MinCardNumberLength = 14;
    private const int MaxCardNumberLength = 19;
    private const int MinCvvLength = 3;
    private const int MaxCvvLength = 4;

    public static readonly IReadOnlySet<string> SupportedCurrencies = new HashSet<string> { "GBP", "USD", "EUR" };

    private readonly TimeProvider _timeProvider;

    public PaymentRequestValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Returns the validation errors keyed by the field's JSON (camelCase) name. An empty dictionary means the request is valid.
    /// </summary>
    public Dictionary<string, string[]> Validate(PostPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, List<string>> errors = new();

        void Fail(string property, string message)
        {
            string field = JsonNamingPolicy.CamelCase.ConvertName(property);
            if (!errors.TryGetValue(field, out List<string>? messages))
            {
                errors[field] = messages = new List<string>();
            }

            messages.Add(message);
        }

        if (string.IsNullOrEmpty(request.CardNumber))
        {
            Fail(nameof(request.CardNumber), "Card number is required.");
        }
        else
        {
            if (request.CardNumber.Length is < MinCardNumberLength or > MaxCardNumberLength)
            {
                Fail(nameof(request.CardNumber), "Card number must be between 14 and 19 characters long.");
            }

            if (!IsNumeric(request.CardNumber))
            {
                Fail(nameof(request.CardNumber), "Card number must only contain numeric characters.");
            }
        }

        bool monthValid = false;
        if (request.ExpiryMonth is null)
        {
            Fail(nameof(request.ExpiryMonth), "Expiry month is required.");
        }
        else if (request.ExpiryMonth is < 1 or > 12)
        {
            Fail(nameof(request.ExpiryMonth), "Expiry month must be between 1 and 12.");
        }
        else
        {
            monthValid = true;
        }

        if (request.ExpiryYear is null)
        {
            Fail(nameof(request.ExpiryYear), "Expiry year is required.");
        }
        else if (monthValid && IsExpired(request.ExpiryMonth!.Value, request.ExpiryYear.Value))
        {
            Fail(nameof(request.ExpiryYear), "Card expiry date must be in the future.");
        }

        if (string.IsNullOrEmpty(request.Currency))
        {
            Fail(nameof(request.Currency), "Currency is required.");
        }
        else if (request.Currency.Length != 3)
        {
            Fail(nameof(request.Currency), "Currency must be 3 characters long.");
        }
        else if (!SupportedCurrencies.Contains(request.Currency))
        {
            Fail(nameof(request.Currency), $"Currency must be one of: {string.Join(", ", SupportedCurrencies)}.");
        }

        if (request.Amount is null)
        {
            Fail(nameof(request.Amount), "Amount is required.");
        }
        else if (request.Amount <= 0)
        {
            Fail(nameof(request.Amount), "Amount must be greater than zero.");
        }

        if (string.IsNullOrEmpty(request.Cvv))
        {
            Fail(nameof(request.Cvv), "CVV is required.");
        }
        else
        {
            if (request.Cvv.Length is < MinCvvLength or > MaxCvvLength)
            {
                Fail(nameof(request.Cvv), "CVV must be 3 or 4 characters long.");
            }

            if (!IsNumeric(request.Cvv))
            {
                Fail(nameof(request.Cvv), "CVV must only contain numeric characters.");
            }
        }

        return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
    }

    private static bool IsNumeric(string value) => value.All(char.IsAsciiDigit);

    // A card stays valid until the end of its expiry month, so the current month is still accepted.
    private bool IsExpired(int month, int year)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        return (year * 12) + month < (now.Year * 12) + now.Month;
    }
}
