using System.Diagnostics;
using Errors.AspNetCore.Classifiers;
using Errors.AspNetCore.Core;
using Errors.AspNetCore.Enrichers;
using Errors.AspNetCore.Formatters;
using Errors.AspNetCore.Mappers;
using Errors.AspNetCore.Registry;
using Errors.AspNetCore.Sanitization;
using Microsoft.Extensions.DependencyInjection;   
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;               
using Observability.OpenTelemetry.Telemetry;

namespace Errors.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for registering the error handling pipeline.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Adds the CleanErrorWorks exception handling infrastructure to the service collection.
    /// </summary>
    /// <param name="services">Target service collection.</param>
    /// <param name="configure">Optional callback for <see cref="ErrorHandlingOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddErrorHandling(this IServiceCollection services, Action<ErrorHandlingOptions>? configure = null)
    {
        var errorHandlingOptions = new ErrorHandlingOptions();
        configure?.Invoke(errorHandlingOptions);

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

                var env = ctx.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                if (!env.IsDevelopment() && ctx.Exception is not null)
                    ctx.ProblemDetails.Detail = "An unexpected error occurred.";
            };
        });

        services.AddOptions<ExceptionSanitizerOptions>();
        services.AddOptions<ProblemDetailsExtensionValidationOptions>();

        if (errorHandlingOptions.ConfigureExceptionSanitizer is not null)
            services.Configure<ExceptionSanitizerOptions>(errorHandlingOptions.ConfigureExceptionSanitizer);

        if (errorHandlingOptions.ConfigureProblemDetailsExtensions is not null)
            services.Configure<ProblemDetailsExtensionValidationOptions>(errorHandlingOptions.ConfigureProblemDetailsExtensions);

        services.AddSingleton<IExceptionSanitizer, ExceptionSanitizer>();
        services.AddSingleton<SqlServerErrorClassifier>();


        services.AddSingleton<IExceptionProblemDetailsMapper, ValidationExceptionMapper>();
        services.AddSingleton<IExceptionProblemDetailsMapper, NotFoundExceptionMapper>();
        services.AddSingleton<IExceptionProblemDetailsMapper, AuthorizationExceptionMapper>();
        services.AddSingleton<IExceptionProblemDetailsMapper, RateLimitExceptionMapper>();
        services.AddSingleton<IExceptionProblemDetailsMapper, DomainExceptionMapper>();
        services.AddSingleton<IExceptionProblemDetailsMapper, SqlExceptionMapper>();     

        services.AddSingleton<IExceptionProblemDetailsMapper, HttpRequestExceptionMapper>();
        services.AddSingleton<IExceptionProblemDetailsMapper, TimeoutRejectedExceptionMapper>();

        services.AddSingleton<IExceptionProblemDetailsMapper, AppErrorFallbackMapper>();

        services.AddSingleton<IExceptionProblemDetailsMapper, UnknownExceptionMapper>();

        foreach (var mapperType in errorHandlingOptions.MapperTypes)
            services.AddSingleton(typeof(IExceptionProblemDetailsMapper), mapperType);

        EnsureFallbackMapperRegistered(services);

        services.AddSingleton<IExceptionMapperRegistry, ExceptionMapperRegistry>();
        services.AddSingleton<IExceptionMapperResolver, ExceptionMapperResolver>();

        services.TryAddSingleton<IActivityTagger, NullActivityTagger>();
        services.TryAddSingleton<IActivityEventFactory, NullActivityEventFactory>();
        services.TryAddSingleton(_ => new ActivitySource("CleanErrorWorks.Diagnostics"));

        services.AddSingleton<ISpanEnricher, ActivitySpanEnricher>();
        services.AddSingleton<IProblemDetailsFormatter, ProblemDetailsFormatter>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    private static void EnsureFallbackMapperRegistered(IServiceCollection services)
    {
        var hasFallback = services.Any(sd =>
            sd.ServiceType == typeof(IExceptionProblemDetailsMapper)
            && sd.ImplementationType is { } implementationType
            && implementationType.GetCustomAttributes(typeof(ExceptionMapperAttribute), false)
                .OfType<ExceptionMapperAttribute>()
                .Any(attr => attr.IsFallback));

        if (!hasFallback)
            throw new InvalidOperationException($"At least one {nameof(IExceptionProblemDetailsMapper)} must be decorated with {nameof(ExceptionMapperAttribute)} and have IsFallback=true.");
    }
}
