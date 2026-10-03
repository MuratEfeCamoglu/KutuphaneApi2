namespace KutuphaneApi.Dtos.Members;

// CreatedAt istekte yok: kayıt tarihini istemci değil sunucu belirler.
public record CreateMemberRequest(string FirstName, string LastName, string Email, string? PhoneNumber);
