using BillingService.Web.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Web.XUnit.Tests.Middlewares
{
    public sealed class CorrelationIdMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_SetsCorrelationIdInHttpContextItems_WhenHeaderPresent()
        {
            const string existingCorrelationId = "test-correlation-id-123";

            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = existingCorrelationId;

            var middleware = new CorrelationIdMiddleware(
                _ => Task.CompletedTask,
                NullLogger<CorrelationIdMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            Assert.Equal(existingCorrelationId, context.Items[CorrelationIdMiddleware.HeaderName]);
        }

        [Fact]
        public async Task InvokeAsync_GeneratesNewCorrelationId_WhenHeaderAbsent()
        {
            var context = new DefaultHttpContext();

            var middleware = new CorrelationIdMiddleware(
                _ => Task.CompletedTask,
                NullLogger<CorrelationIdMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            var correlationId = context.Items[CorrelationIdMiddleware.HeaderName] as string;
            Assert.NotNull(correlationId);
            Assert.True(Guid.TryParseExact(correlationId, "D", out _),
                "Generated correlation ID should be a valid GUID.");
        }

        [Fact]
        public async Task InvokeAsync_EchoesCorrelationIdInResponseHeader()
        {
            const string existingCorrelationId = "echo-test-id";

            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = existingCorrelationId;

            var middleware = new CorrelationIdMiddleware(
                _ => Task.CompletedTask,
                NullLogger<CorrelationIdMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            Assert.Equal(
                existingCorrelationId,
                context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        }

        [Fact]
        public async Task InvokeAsync_CallsNextMiddleware()
        {
            var nextCalled = false;
            var context = new DefaultHttpContext();

            var middleware = new CorrelationIdMiddleware(
                _ => { nextCalled = true; return Task.CompletedTask; },
                NullLogger<CorrelationIdMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_GeneratesUniqueCorrelationIds_ForDistinctRequests()
        {
            var context1 = new DefaultHttpContext();
            var context2 = new DefaultHttpContext();

            var middleware = new CorrelationIdMiddleware(
                _ => Task.CompletedTask,
                NullLogger<CorrelationIdMiddleware>.Instance);

            await middleware.InvokeAsync(context1);
            await middleware.InvokeAsync(context2);

            var id1 = context1.Items[CorrelationIdMiddleware.HeaderName] as string;
            var id2 = context2.Items[CorrelationIdMiddleware.HeaderName] as string;

            Assert.NotNull(id1);
            Assert.NotNull(id2);
            Assert.NotEqual(id1, id2);
        }
    }
}
