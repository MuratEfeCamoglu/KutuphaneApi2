namespace KutuphaneApi.Common.Exceptions;

// İş kuralı ihlali (stok yok, benzersiz alan tekrarı, silinemeyen kayıt...). GlobalExceptionHandler bunu 409'a çevirir.
public class BusinessRuleException(string message) : Exception(message);
