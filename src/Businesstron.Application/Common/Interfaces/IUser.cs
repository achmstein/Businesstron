namespace Businesstron.Application.Common.Interfaces;

/// <summary>The current authenticated user, surfaced from the web layer.</summary>
public interface IUser
{
    string? Id { get; }
    string? Email { get; }
}
