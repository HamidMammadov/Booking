using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Booking.Api.Middleware
{
    public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
    {
        private const string CorrelationHeader = "X-Correlation-Id";

        public async Task Invoke(HttpContext context)
        {
            var correlationId = EnsureCorrelationId(context);
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private string EnsureCorrelationId(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(CorrelationHeader, out StringValues values) || StringValues.IsNullOrEmpty(values))
            {
                var generated = Guid.NewGuid().ToString("n");
                context.Request.Headers[CorrelationHeader] = generated;
                context.Response.Headers[CorrelationHeader] = generated;
                return generated;
            }
            context.Response.Headers[CorrelationHeader] = values.ToString();
            return values.ToString();
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex, string correlationId)
        {
            var status = StatusCodes.Status500InternalServerError;
            string title = "An unexpected error occurred.";
            string type = "https://httpstatuses.io/500";
            string? detail = env.IsDevelopment() ? ex.ToString() : null;

            IDictionary<string, string[]>? errors = null;

            switch (ex)
            {
                case ValidationException fv:
                    status = StatusCodes.Status400BadRequest;
                    title = "One or more validation errors occurred.";
                    type = "https://datatracker.ietf.org/doc/html/rfc9110#name-400-bad-request";
                    errors = fv.Errors
                        .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "request" : e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    detail = env.IsDevelopment() ? fv.ToString() : null;
                    break;

                case OperationCanceledException:
                    status = 499;
                    title = "Request was cancelled.";
                    type = "about:blank";
                    detail = null;
                    break;

                default:
                    logger.LogError(ex, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);
                    break;
            }

            ProblemDetails body;
            if (errors is not null)
            {
                body = new ValidationProblemDetails(errors);
            }
            else
            {
                body = new ProblemDetails();
            }

            body.Status = status;
            body.Title = title;
            body.Detail = detail;
            body.Type = type;
            body.Instance = context.Request.Path;

            body.Extensions["traceId"] = context.TraceIdentifier;
            body.Extensions["correlationId"] = correlationId;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json; charset=utf-8";

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
