using Microsoft.Extensions.DependencyInjection;   
using Microsoft.Extensions.Hosting;               

namespace Errors.AspNetCore;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
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

        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}