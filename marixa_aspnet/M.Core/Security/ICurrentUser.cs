namespace M.Core.Security;

/// <summary>The authenticated actor, independent of HTTP and token implementations.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? EmployeeId { get; }
    string Name { get; }
    bool IsInRole(string role);
}
