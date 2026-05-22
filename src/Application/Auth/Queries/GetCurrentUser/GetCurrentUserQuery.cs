using LinguaSpace.Application.Auth.DTOs;
using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace LinguaSpace.Application.Auth.Queries.GetCurrentUser;

[Authorize]
public record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        IUser currentUser)
    {
        _context = context;
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        UserProfile profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        string email = await _identityService.GetEmailAsync(userId) ?? string.Empty;
        bool isEmailConfirmed = await _identityService.IsEmailConfirmedAsync(userId);

        IList<string> roles = _currentUser.Roles?.ToList() ?? [];

        return new CurrentUserDto(userId, email, profile.DisplayName, roles, profile.AvatarUrl, isEmailConfirmed);
    }
}
