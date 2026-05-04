using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Constants;

namespace LinguaSpace.Application.Moderation.Commands.BanUser;

/// <summary>
/// Locks out a user account. Pass null for <paramref name="Until"/> for a permanent ban. Admin only.
/// </summary>
public record BanUserCommand(string TargetUserId, DateTimeOffset? Until) : IRequest;

[Authorize(Roles = Roles.Administrator)]
public class BanUserCommandHandler : IRequestHandler<BanUserCommand>
{
    private readonly IIdentityService _identityService;

    public BanUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        Result result = await _identityService.LockoutUserAsync(request.TargetUserId, request.Until);

        if (!result.Succeeded)
        {
            throw new NotFoundException("User", request.TargetUserId);
        }
    }
}
