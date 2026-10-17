using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Data.SqlClient;
﻿using M.Core.Base;
using System.Net;

namespace M.API.Middleware
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BaseException.ErrorException ex)
            {
                await HandleExceptionAsync(context, ex.StatusCode, ex.ErrorDetail.ErrorCode, ex.ErrorDetail.ErrorMessage?.ToString());
            }
            catch (DbUpdateConcurrencyException)
            {
                await HandleExceptionAsync(context, 409, "CONCURRENT_CHANGE", "A concurrent change was detected. Please retry.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 547 }
                or PostgresException { SqlState: "23505" or "23503" or "23514" or "23502" }
                || ex.InnerException?.GetType().Name == "SqliteException")
            {
                await HandleExceptionAsync(context, 409, "PERSISTENCE_CONFLICT", "The change conflicts with existing data. Refresh and retry.");
            }
            catch (PostgresException ex) when (ex.SqlState is "40001" or "40P01")
            {
                await HandleExceptionAsync(context, 409, "CONCURRENT_CHANGE", "A concurrent change was detected. Please retry.");
            }
            catch (SqlException ex) when (ex.Number == 1205)
            {
                await HandleExceptionAsync(context, 409, "CONCURRENT_CHANGE", "A concurrent change was detected. Please retry.");
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The client has disconnected. Do not turn cancellation into a server error.
            }
            catch (Exception ex)
            {
                logger.LogError("Unhandled API exception ({ExceptionType}) for {Method} {Path}; trace {TraceId}",
                    ex.GetType().Name, context.Request.Method, context.Request.Path, context.TraceIdentifier);
                await HandleExceptionAsync(context, (int)HttpStatusCode.InternalServerError,
                    ResponseCodeConstants.INTERNAL_SERVER_ERROR, "An unexpected error occurred");
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, int statusCode, string? errorCode, object? errorMessage)
        {
            if (context.Response.HasStarted) return Task.CompletedTask;
            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new
            {
                statusCode,
                code = errorCode,
                message = errorMessage,
                data = (object?)null,
                traceId = context.TraceIdentifier
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
