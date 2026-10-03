namespace KutuphaneApi.Dtos.Authors;

// DTO (Data Transfer Object): API'nin dışarıya gösterdiği veri şekli. Entity'yi doğrudan döndürmeyiz.
// record: değer eşitliği olan, kısa yazılan, değişmez (immutable) veri tipi.
public record AuthorDto(int Id, string FirstName, string LastName, int? BirthYear, int BookCount);
