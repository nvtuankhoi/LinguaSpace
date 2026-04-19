using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetUserProfile;

[Authorize]
public record GetUserProfileQuery(string UserId) : IRequest<UserProfileDto>;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        UserProfile profile = await _context.UserProfiles
            .Include(p => p.Languages)
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.UserId);

        return MapToDto(profile);
    }

    private static UserProfileDto MapToDto(UserProfile p) => new(
        p.Id,
        p.UserId,
        p.DisplayName,
        p.Bio,
        p.AvatarUrl,
        p.Timezone,
        p.IsOnline,
        p.LastSeenAt,
        p.Languages.Select(l => new UserLanguageDto(l.Id, l.LanguageCode, l.Type.ToString(), l.Level?.ToString())).ToList());
}
