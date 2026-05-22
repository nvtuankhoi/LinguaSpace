using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Constants;

namespace LinguaSpace.Application.Moderation.Commands.UnbanUser;

[Authorize(Roles = Roles.Administrator)]
public record UnbanUserCommand(string UserId) : IRequest;

public class UnbanUserCommandHandler : IRequestHandler<UnbanUserCommand>
{
    private readonly IIdentityService _identityService;

    public UnbanUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(UnbanUserCommand request, CancellationToken cancellationToken)
    {
        Result result = await _identityService.UnlockUserAsync(request.UserId);

        if (!result.Succeeded)
        {
            throw new NotFoundException("User", request.UserId);
        }
    }
}
