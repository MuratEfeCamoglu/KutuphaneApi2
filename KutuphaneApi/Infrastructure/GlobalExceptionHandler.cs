using FluentValidation;
using KutuphaneApi.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Infrastructure;

// IExceptionHandler: uygulamanın herhangi bir yerinde yakalanmamış exception buraya düşer.
// Controller'larda try-catch yazmak yerine hataları tek yerden HTTP yanıtına çeviririz.
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            // ValidationProblemDetails: ProblemDetails'e alan bazlı "errors" sözlüğü ekler.
            ValidationException validationException => new ValidationProblemDetails(
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Doğrulama hatası"
            },
            NotFoundException => Create(StatusCodes.Status404NotFound, "Kayıt bulunamadı", exception.Message),
            BusinessRuleException => Create(StatusCodes.Status409Conflict, "İş kuralı ihlali", exception.Message),
            // 500 hatalarında iç ayrıntıyı (stack trace, SQL) istemciye sızdırmıyoruz.
            _ => Create(StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu", null)
        };

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "İşlenmeyen hata: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        // ProblemDetails: hata yanıtları için standart JSON biçimi (RFC 9457): type, title, status, detail.
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static ProblemDetails Create(int statusCode, string title, string? detail) => new()
    {
        Status = statusCode,
        Title = title,
        Detail = detail
    };
}
