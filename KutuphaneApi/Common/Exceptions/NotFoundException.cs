namespace KutuphaneApi.Common.Exceptions;

// Özel exception: servis "kayıt yok" dediğinde fırlatır, GlobalExceptionHandler bunu 404'e çevirir.
public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} bulunamadı (Id: {key}).");
