using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KutuphaneApi.Data;

// ValueConverter: bir değeri veritabanına yazarken ve okurken dönüştürür.
// Yazarken değer olduğu gibi kalır, okurken Kind = Utc olarak işaretlenir (JSON'da sonuna "Z" eklenir).
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
