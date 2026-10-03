using KutuphaneApi.Dtos.Authors;
using KutuphaneApi.Entities;

namespace KutuphaneApi.Mappings;

// Elle yazılmış eşleme (mapping) metotları. AutoMapper yerine açık ve izlenebilir kod.
public static class AuthorMappings
{
    // IQueryable üzerinde çalışan projeksiyon: EF Core bunu SQL'e çevirir, sadece gereken sütunları okur.
    // a.Books.Count da SQL'de bir COUNT alt sorgusu olur; kitaplar belleğe yüklenmez.
    public static IQueryable<AuthorDto> SelectDto(this IQueryable<Author> query) =>
        query.Select(a => new AuthorDto(a.Id, a.FirstName, a.LastName, a.BirthYear, a.Books.Count));

    public static Author ToEntity(this CreateAuthorRequest request) => new()
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        BirthYear = request.BirthYear
    };

    public static void ApplyTo(this UpdateAuthorRequest request, Author author)
    {
        author.FirstName = request.FirstName;
        author.LastName = request.LastName;
        author.BirthYear = request.BirthYear;
    }
}
