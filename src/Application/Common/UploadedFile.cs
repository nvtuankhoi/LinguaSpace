namespace LinguaSpace.Application.Common;

/// <summary>
/// Result of a successful upload: the public URL of the stored file and its content type.
/// </summary>
public sealed record UploadedFile(string Url, string ContentType);
