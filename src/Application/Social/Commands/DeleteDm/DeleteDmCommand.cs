using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Social.Commands.DeleteDm;

[Authorize]
public record DeleteDmCommand(int MessageId) : IRequest;

public class DeleteDmCommandHandler : IRequestHandler<DeleteDmCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DeleteDmCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteDmCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        DirectMessage message = await _context.DirectMessages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectMessage), request.MessageId);

        if (message.SenderId != userId)
        {
            throw new ForbiddenAccessException();
        }

        message.IsDeleted = true;
        message.Content = "[deleted]";

        await _context.SaveChangesAsync(cancellationToken);
    }
}
