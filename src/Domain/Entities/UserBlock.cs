namespace LinguaSpace.Domain.Entities;

public class UserBlock : BaseAuditableEntity
{
    public string BlockerId { get; set; } = string.Empty;

    public string BlockedId { get; set; } = string.Empty;
}
