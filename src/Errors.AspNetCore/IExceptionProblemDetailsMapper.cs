using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Errors.AspNetCore;

public interface IExceptionProblemDetailsMapper
{
    bool CanHandle(Exception ex);
    (HttpStatusCode Status, ProblemDetails Details) Map(HttpContext ctx, Exception ex);
}