using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Gamification.Queries.GetXpHistory;

[Authorize]
public record GetXpHistoryQuery(string Period = "week") : IRequest<IList<XpDailyDto>>;

public record XpDailyDto(DateOnly Date, int TotalXp, IList<XpTransactionDto> Transactions);

public record XpTransactionDto(int Amount, string Reason, DateTimeOffset EarnedAt);

public class GetXpHistoryQueryHandler : IRequestHandler<GetXpHistoryQuery, IList<XpDailyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetXpHistoryQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IList<XpDailyDto>> Handle(GetXpHistoryQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        DateTimeOffset cutoff = request.Period.ToLowerInvariant() switch
        {
            "month" => DateTimeOffset.UtcNow.AddDays(-30),
            _ => DateTimeOffset.UtcNow.AddDays(-7)
        };

        IList<XpTransaction> transactions = await _context.XpTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.EarnedAt >= cutoff)
            .OrderBy(t => t.EarnedAt)
            .ToListAsync(cancellationToken);

        IList<XpDailyDto> result = transactions
            .GroupBy(t => DateOnly.FromDateTime(t.EarnedAt.DateTime))
            .OrderBy(g => g.Key)
            .Select(g => new XpDailyDto(
                Date: g.Key,
                TotalXp: g.Sum(t => t.Amount),
                Transactions: g
                    .Select(t => new XpTransactionDto(t.Amount, t.Reason, t.EarnedAt))
                    .ToList()))
            .ToList();

        return result;
    }
}
