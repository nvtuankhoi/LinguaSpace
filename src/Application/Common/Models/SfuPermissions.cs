namespace LinguaSpace.Application.Common.Models;

/// <summary>
/// Permissions granted to a participant in a LiveKit room.
/// Mapped from <see cref="Domain.Enums.ParticipantRole"/> before passing to ISfuService.
/// </summary>
public record SfuPermissions(
    bool CanPublishAudio,
    bool CanPublishVideo,
    bool CanPublishScreen,
    bool CanSubscribe,
    bool CanPublishData)
{
    public static SfuPermissions ForHost() =>
        new(CanPublishAudio: true, CanPublishVideo: true, CanPublishScreen: true,
            CanSubscribe: true, CanPublishData: true);

    public static SfuPermissions ForSpeaker() =>
        new(CanPublishAudio: true, CanPublishVideo: true, CanPublishScreen: false,
            CanSubscribe: true, CanPublishData: true);

    public static SfuPermissions ForListener() =>
        new(CanPublishAudio: false, CanPublishVideo: false, CanPublishScreen: false,
            CanSubscribe: true, CanPublishData: false);
}
