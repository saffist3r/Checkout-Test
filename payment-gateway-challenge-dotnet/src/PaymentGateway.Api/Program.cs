using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Services.Bank;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        // Malformed bodies (e.g. a decimal amount or a string expiry month) are rejected in the same shape as validation failures.
        options.InvalidModelStateResponseFactory = context =>
        {
            // JSON errors arrive keyed "$.amount" with a framework message; expose them as "amount" with a plain message.
            // The "request" entry only repeats that the body could not be bound, so it is dropped when a field error exists.
            Dictionary<string, string[]> errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key.StartsWith("$.") ? entry.Key[2..] : entry.Key,
                    entry => entry.Key.StartsWith("$.")
                        ? new[] { "The value is not valid for this field." }
                        : entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            if (errors.Count > 1)
            {
                errors.Remove("request");
            }
            return new BadRequestObjectResult(new RejectedPaymentResponse(errors));
        };
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PaymentsRepository>();
builder.Services.AddSingleton<PaymentRequestValidator>();
builder.Services.AddScoped<PaymentsService>();
builder.Services.AddHttpClient<IAcquiringBankClient, AcquiringBankClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AcquiringBank:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
