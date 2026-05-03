using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Feed.Commands.CreatePost;

public record CreatePostCommand(
    string Content,
    string PostType,
    string? LanguageCode,
    string? Metadata,
    IList<string>? Tags,
    IList<string>? MediaUrls) : IRequest<int>;

public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PostType).NotEmpty();
        RuleFor(x => x.Tags).Must(t => t == null || t.Count <= 5)
            .WithMessage("Maximum 5 tags per post.");
        RuleFor(x => x.MediaUrls).Must(m => m == null || m.Count <= 4)
            .WithMessage("Maximum 4 media items per post.");
    }
}

[Authorize]
public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreatePostCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        string authorId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        if (!Enum.TryParse(request.PostType, ignoreCase: true, out PostType postType))
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.PostType),
                    $"Invalid PostType: {request.PostType}.")
            ]);
        }

        Post post = new()
        {
            AuthorId = authorId,
            Content = request.Content,
            PostType = postType,
            LanguageCode = request.LanguageCode,
            Metadata = request.Metadata,
        };

        if (request.Tags is not null)
        {
            foreach (string tag in request.Tags.Take(5))
            {
                post.Tags.Add(new PostTag { Tag = tag.ToLowerInvariant() });
            }
        }

        if (request.MediaUrls is not null)
        {
            int sortOrder = 0;
            foreach (string url in request.MediaUrls.Take(4))
            {
                post.MediaItems.Add(new PostMediaItem { Url = url, SortOrder = sortOrder++ });
            }
        }

        _context.Posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        post.AddDomainEvent(new PostCreatedEvent(post.Id, authorId));
        await _context.SaveChangesAsync(cancellationToken);

        return post.Id;
    }
}
