namespace Domain.Identity;

public static class UserRoleExtensions
{
    public const string CustomerRoleName = "Customer";
    public const string AdminRoleName = "Admin";

    public static string ToAuthorizationRole(this UserRole role) => role switch
    {
        UserRole.Admin => AdminRoleName,
        UserRole.Costumer => CustomerRoleName,
        _ => CustomerRoleName
    };
}
