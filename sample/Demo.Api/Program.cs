using Demo.Api;
using Demo.Api.Payments;
using Demo.Api.Stubs;
using Errors.Abstractions.Exceptions;
using Errors.AspNetCore.Extensions;
using Errors.Logging;
using Microsoft.Extensions.Http.Resilience;
using Observability.OpenTelemetry;
using Polly;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddErrorHandling(options =>
{
    // Register a consumer-defined mapper with explicit priority; the built-in UnknownExceptionMapper remains the
    // fallback to avoid registry errors when nothing matches.
    options.RegisterMapper<PaymentDeclinedExceptionMapper>();
});
builder.Services.AddDefaultLogging(builder.Configuration);
builder.Services.AddDefaultOpenTelemetry(builder.Configuration);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<WireMockHostedService>();
}
builder.Services.AddSingleton<SqlTestService>();

builder.Services.AddHttpClient<ExternalClient>(c =>
{
    c.BaseAddress = new Uri("http://localhost:9091");
})
.AddResilienceHandler("custom", r =>
{
    r.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 1,                  
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
    });

    r.AddTimeout(TimeSpan.FromSeconds(2));    
});

var app = builder.Build();


app.UseExceptionHandler();
app.UseStatusCodePages();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapControllers();

// Endpoints
app.MapGet("/ok", () => Results.Ok(new { message = "it works", traceId = Guid.NewGuid() }))
.WithName("Ok");


app.MapGet("/boom", () =>
{
    throw new Exception("Boom! Something unexpected happened");
});


app.MapGet("/not-found/{id}", (int id) =>
{
    throw new NotFoundException($"Resource with id={id} was not found");
});


app.MapGet("/unauthorized", () =>
{
    throw new AuthorizationException("Missing or invalid credentials", forbidden: false);
});


app.MapGet("/forbidden", () =>
{
    throw new AuthorizationException("You do not have access to this resource", forbidden: true);
});


app.MapGet("/validation", () =>
{
    var errors = new Dictionary<string, string[]>
    {
        ["amount"] = ["Amount must be greater than zero"],
        ["currency"] = ["Currency is required"]
    };
    throw new ValidationException("Invalid payload", errors);
});


app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/sql/duplicate", async (SqlTestService s) => { await s.TriggerDuplicateAsync(); return Results.Ok("If you see this, it didn't duplicate (run again)."); });
app.MapPost("/sql/constraint", async (SqlTestService s) => { await s.TriggerConstraintAsync(); return Results.Ok("If you see this, constraint didn't fail (weird)."); });
app.MapPost("/sql/timeout", async (SqlTestService s) => { await s.TriggerTimeoutAsync(); return Results.Ok("Timeout not triggered; increase WAITFOR."); });
app.MapPost("/sql/deadlock", async (SqlTestService s) => { await s.TriggerDeadlockAsync(); return Results.Ok("No deadlock (rare); try again."); });
app.MapPost("/sql/dbunavailable", async (SqlTestService s) => { await s.TriggerDbUnavailableAsync(); return Results.Ok("Unexpectedly opened bad DB."); });


app.MapGet("/ext/ok", async (ExternalClient c) => Results.Text(await c.GetAsync("/ok")));
app.MapGet("/ext/slow", async (ExternalClient c) => Results.Text(await c.GetAsync("/slow")));  
app.MapGet("/ext/flaky", async (ExternalClient c) => Results.Text(await c.GetAsync("/flaky"))); 
app.MapGet("/ext/rate", async (ExternalClient c) => Results.Text(await c.GetAsync("/rate"))); 
app.MapGet("/ext/boom", async (ExternalClient c) => Results.Text(await c.GetAsync("/boom")));

app.MapPost("/payments/decline", (string? reason) =>
{
    // This demonstrates how a consumer-provided exception flows through the mapper registry.
    var detail = string.IsNullOrWhiteSpace(reason)
        ? "Payment was declined by the issuer"
        : reason;

    throw new PaymentDeclinedException(detail, isTransient: false, preferredStatus: System.Net.HttpStatusCode.PaymentRequired);
});

app.Run();