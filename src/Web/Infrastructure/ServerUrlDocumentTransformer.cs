using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LinguaSpace.Web.Infrastructure;

/// <summary>
/// Removes trailing slashes from OpenAPI server URLs.
/// Without this, some HTTP clients combine "https://host:7241/" + "/api/Auth/login"
/// and produce "https://host:7241//api/Auth/login" (double slash).
/// </summary>
internal sealed class ServerUrlDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Servers is not null)
        {
            foreach (OpenApiServer server in document.Servers)
            {
                if (!string.IsNullOrEmpty(server.Url) && server.Url.EndsWith('/'))
                {
                    server.Url = server.Url.TrimEnd('/');
                }
            }
        }

        return Task.CompletedTask;
    }
}
