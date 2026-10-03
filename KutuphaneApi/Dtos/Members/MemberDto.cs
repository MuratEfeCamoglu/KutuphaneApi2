namespace KutuphaneApi.Dtos.Members;

public record MemberDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateTime CreatedAt,
    int ActiveLoanCount);
