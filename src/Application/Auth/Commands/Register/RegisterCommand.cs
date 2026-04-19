using LinguaSpace.Application.Auth.DTOs;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Auth.Commands.Register;

public record RegisterCommand(string Email, string Password) : IRequest<RegisterResult>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IIdentityService _identityService;
    private readonly IPublisher _publisher;

    public RegisterCommandHandler(IIdentityService identityService, IPublisher publisher)
    {
        _identityService = identityService;
        _publisher = publisher;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        (Result result, string userId) = await _identityService.CreateUserAsync(request.Email, request.Password);

        result.ThrowOnFailure();

        await _publisher.Publish(new UserRegisteredEvent(userId, request.Email), cancellationToken);

        return new RegisterResult(userId, request.Email);
    }
}
