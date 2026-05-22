using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LinguaSpace.Web.Infrastructure;

/// <summary>
/// Overrides the global document-level Bearer security requirement for endpoints that do
/// NOT require authentication, by setting <c>security: []</c> (anonymous override).
/// All other endpoints inherit the global <c>security: [{"Bearer":[]}]</c> set by
/// <see cref="BearerSecuritySchemeTransformer"/> without any operation-level entry needed.
/// </summary>
internal sealed class BearerSecurityOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        bool requiresAuth = context.Description.ActionDescriptor.EndpointMetadata
            .Any(m => m is IAuthorizeData);

        if (!requiresAuth)
        {
            // Override global security: this endpoint is public (no auth required).
            // OpenAPI 3.x: security: [] at operation level means "no security required",
            // which overrides the document-level default.
            operation.Security = [];
        }

        return Task.CompletedTask;
    }
}
