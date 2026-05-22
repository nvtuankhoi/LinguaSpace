using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetMyLanguages;

[Authorize]
public record GetMyLanguagesQuery : IRequest<IList<UserLanguageDto>>;

public class GetMyLanguagesQueryHandler : IRequestHandler<GetMyLanguagesQuery, IList<UserLanguageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetMyLanguagesQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IList<UserLanguageDto>> Handle(GetMyLanguagesQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        return await _context.UserLanguages
            .AsNoTracking()
            .Where(l => l.UserProfile.UserId == userId)
            .OrderBy(l => l.LanguageCode)
            .Select(l => new UserLanguageDto(
                l.Id,
                l.LanguageCode,
                l.Type.ToString(),
                l.Level != null ? l.Level.ToString() : null))
            .ToListAsync(cancellationToken);
    }
}
