using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            // Yakalanan hatayı logla
            _logger.LogError(ex, "Beklenmeyen bir hata oluştu.");
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Standart bir format dönüyorum
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.com/500",
            Title = "Sunucu Hatası",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "İsteğiniz işlenirken beklenmedik bir hata oluştu.",
            Instance = context.Request.Path
        };

        // Eğer geliştirme ortamındaysak, daha fazla detay ver
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions.Add("stackTrace", exception.StackTrace);
        }

        var jsonResponse = JsonSerializer.Serialize(problemDetails);
        return context.Response.WriteAsync(jsonResponse);
    }
}
