using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Users.Commands.UpdateLanguage;

[Authorize]
public record UpdateLanguageCommand(int LanguageId, LanguageLevel? Level) : IRequest;

public class UpdateLanguageCommandHandler : IRequestHandler<UpdateLanguageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateLanguageCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateLanguageCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        UserLanguage language = await _context.UserLanguages
            .Include(l => l.UserProfile)
            .FirstOrDefaultAsync(l => l.Id == request.LanguageId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserLanguage), request.LanguageId.ToString());

        if (language.UserProfile.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        language.Level = request.Level;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
