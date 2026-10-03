using KutuphaneApi.Dtos.Members;
using KutuphaneApi.Entities;

namespace KutuphaneApi.Mappings;

public static class MemberMappings
{
    public static IQueryable<MemberDto> SelectDto(this IQueryable<Member> query) =>
        query.Select(m => new MemberDto(
            m.Id,
            m.FirstName,
            m.LastName,
            m.Email,
            m.PhoneNumber,
            m.CreatedAt,
            m.Loans.Count(l => l.ReturnDate == null)));

    public static Member ToEntity(this CreateMemberRequest request, DateTime createdAt) => new()
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        PhoneNumber = request.PhoneNumber,
        CreatedAt = createdAt
    };

    public static void ApplyTo(this UpdateMemberRequest request, Member member)
    {
        member.FirstName = request.FirstName;
        member.LastName = request.LastName;
        member.Email = request.Email;
        member.PhoneNumber = request.PhoneNumber;
    }
}
