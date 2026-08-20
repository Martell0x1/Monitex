using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SmartHome.Middlewares;

namespace smart_home.UnitTests.Middleware;

public class RequestLoggingMiddlewareUnitTest
{
    [Fact]
    public async Task InvokeAsync_LogsIncomingAndOutgoingRequest()
    {
        var logger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        };
        var middleware = new RequestLoggingMiddleware(next, logger.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(204, context.Response.StatusCode);
        logger.VerifyLogContains(LogLevel.Information, "Incomming Request");
        logger.VerifyLogContains(LogLevel.Information, "Outgoing Request");
    }

    [Fact]
    public void UseRequestLogging_RegistersMiddleware()
    {
        var app = new Mock<IApplicationBuilder>();
        app.Setup(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>())).Returns(app.Object);

        var result = app.Object.UseRequestLogging();

        Assert.Same(app.Object, result);
        app.Verify(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }
}

internal static class LoggerMoqExtensions
{
    public static void VerifyLogContains<T>(this Mock<ILogger<T>> logger, LogLevel level, string fragment)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(fragment, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
