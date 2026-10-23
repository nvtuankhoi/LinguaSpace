using System.Text.Json;

namespace LinguaSpace.Application.Feed.DTOs;

public record PostMetadataDto(
    string? AudioUrl,
    int? DurationSeconds,
    string? ThumbnailUrl,
    string? LinkUrl,
    string? LinkTitle,
    string? LinkDescription,
    string? BackText,
    string? Pronunciation,
    string? Example)
{
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web);

    public static PostMetadataDto? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PostMetadataDto>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
