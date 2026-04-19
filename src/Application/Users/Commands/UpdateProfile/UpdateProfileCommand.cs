using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.UpdateProfile;

[Authorize]
public record UpdateProfileCommand(
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Timezone) : IRequest;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateProfileCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        UserProfile profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        profile.DisplayName = request.DisplayName;
        profile.Bio = request.Bio;
        profile.AvatarUrl = request.AvatarUrl;
        profile.Timezone = request.Timezone;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
