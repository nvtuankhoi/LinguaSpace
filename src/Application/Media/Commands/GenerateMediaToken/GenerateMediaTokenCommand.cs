using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Media.DTOs;
using Microsoft.Extensions.Configuration;

namespace LinguaSpace.Application.Media.Commands.GenerateMediaToken;

[Authorize]
public record GenerateMediaTokenCommand(int RoomId) : IRequest<MediaTokenDto>;

public class GenerateMediaTokenCommandHandler : IRequestHandler<GenerateMediaTokenCommand, MediaTokenDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ISfuService _sfuService;
    private readonly IConfiguration _configuration;

    public GenerateMediaTokenCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        ISfuService sfuService,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _sfuService = sfuService;
        _configuration = configuration;
    }

    public async Task<MediaTokenDto> Handle(
        GenerateMediaTokenCommand request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        if (room.RoomType == RoomType.TextOnly)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.RoomId), "Room is text-only; media sessions are not supported.")
            ]);
        }

        RoomParticipant participant = room.Participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new ForbiddenAccessException();

        SfuPermissions permissions = participant.Role switch
        {
            ParticipantRole.Host => SfuPermissions.ForHost(),
            ParticipantRole.Speaker => SfuPermissions.ForSpeaker(),
            _ => SfuPermissions.ForListener()
        };

        // Generate a LiveKit room name from the numeric Room ID if not already set
        string livekitRoomName = room.LiveKitRoomName ?? $"room-{room.Id}";

        string token = await _sfuService.GenerateTokenAsync(
            livekitRoomName,
            userId,
            _currentUser.UserName ?? userId,
            permissions,
            cancellationToken);

        string livekitUrl = _configuration["LiveKit:Host"] ?? "ws://localhost:7880";

        return new MediaTokenDto(token, livekitUrl);
    }
}
