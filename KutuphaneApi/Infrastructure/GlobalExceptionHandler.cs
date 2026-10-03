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
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Kayıt bulunamadı"),
            BusinessRuleException => (StatusCodes.Status409Conflict, "İş kuralı ihlali"),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "İşlenmeyen hata: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        // ProblemDetails: hata yanıtları için standart JSON biçimi (RFC 9457): type, title, status, detail.
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                // 500 hatalarında iç ayrıntıyı (stack trace, SQL) istemciye sızdırmıyoruz.
                Detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message
            }
        });
    }
}
