namespace KutuphaneApi.Dtos.Members;

public record UpdateMemberRequest(string FirstName, string LastName, string Email, string? PhoneNumber);
