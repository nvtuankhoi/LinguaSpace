using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.UpdateAvatar;

/// <summary>
/// Updates the user's avatar URL.
///
/// Accepts a URL string directly. The client uploads the image via
/// <c>POST /api/Users/me/avatar/upload</c> (which calls IStorageService.UploadAsync) and
/// then persists the returned URL here (or via UpdateProfile.avatarUrl).
/// </summary>
[Authorize]
public record UpdateAvatarCommand(string AvatarUrl) : IRequest;

public class UpdateAvatarCommandHandler : IRequestHandler<UpdateAvatarCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateAvatarCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAvatarCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        UserProfile profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        profile.AvatarUrl = request.AvatarUrl;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
