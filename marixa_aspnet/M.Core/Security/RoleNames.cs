namespace M.Core.Security;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string HR = "HR";
    public const string Employee = "Employee";
    public const string Accountant = "Accountant";
    public const string Sale = "Sale";
    public const string Marketing = "Marketing";
    public const string Staff = Admin + "," + Manager + "," + HR;
}
