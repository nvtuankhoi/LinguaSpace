using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Social.Commands.EditDm;

[Authorize]
public record EditDmCommand(int MessageId, string NewContent) : IRequest;

public class EditDmCommandValidator : AbstractValidator<EditDmCommand>
{
    public EditDmCommandValidator()
    {
        RuleFor(x => x.NewContent).NotEmpty().MaximumLength(2000);
    }
}

public class EditDmCommandHandler : IRequestHandler<EditDmCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public EditDmCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(EditDmCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        DirectMessage message = await _context.DirectMessages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId && !m.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectMessage), request.MessageId);

        if (message.SenderId != userId)
        {
            throw new ForbiddenAccessException();
        }

        message.Content = request.NewContent;
        message.EditedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
