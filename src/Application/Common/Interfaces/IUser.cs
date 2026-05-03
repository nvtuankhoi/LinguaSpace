namespace LinguaSpace.Application.Common.Interfaces;

public interface IUser
{
    string? Id { get; }
    string? UserName { get; }
    List<string>? Roles { get; }
}
