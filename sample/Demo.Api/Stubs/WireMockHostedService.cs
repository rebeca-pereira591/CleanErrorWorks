using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;

namespace Demo.Api.Stubs;

public sealed class WireMockHostedService : IHostedService
{
    private WireMockServer? _server;

    public Task StartAsync(CancellationToken ct)
    {
        _server = WireMockServer.Start(9091);

        _server.Given(Request.Create().WithPath("/ok").UsingGet())
               .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { msg = "ok" }));

        _server.Given(Request.Create().WithPath("/slow").UsingGet())
               .RespondWith(Response.Create()
                   .WithDelay(TimeSpan.FromSeconds(20))
                   .WithStatusCode(200)
                   .WithBody("slow..."));

        _server.Given(Request.Create().WithPath("/flaky").UsingGet())
               .RespondWith(Response.Create().WithCallback(_ =>
               {
                   var code = Random.Shared.Next(2) == 0 ? 500 : 200;
                   return new WireMock.ResponseMessage
                   {
                       StatusCode = code,
                       BodyData = new WireMock.Util.BodyData
                       {
                           DetectedBodyType = BodyType.String,
                           BodyAsString = "maybe error"
                       }
                   };
               }));

        _server.Given(Request.Create().WithPath("/rate").UsingGet())
               .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "2").WithBody("too many"));

        _server.Given(Request.Create().WithPath("/boom").UsingGet())
               .RespondWith(Response.Create().WithStatusCode(503).WithBody("down"));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _server?.Stop();
        _server?.Dispose();
        return Task.CompletedTask;
    }
}