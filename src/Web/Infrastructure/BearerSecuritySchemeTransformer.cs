using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LinguaSpace.Web.Infrastructure;

/// <summary>
/// Adds the Bearer JWT security scheme to the OpenAPI document components AND sets
/// global document-level security to <c>{"Bearer":[]}</c>, so the spec defaults to
/// requiring authentication on every endpoint.
/// Public endpoints override this with <c>security: []</c> via
/// <see cref="BearerSecurityOperationTransformer"/>.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        IEnumerable<AuthenticationScheme> authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };

            // Set global security at document root so every endpoint requires Bearer by default.
            // Public endpoints explicitly override this with security: [] via the operation transformer.
            document.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer")] = [],
                },
            ];
        }
    }
}
