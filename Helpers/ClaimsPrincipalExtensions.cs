using System.Security.Claims;

namespace HRM.Web.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roles)
    {
        if (user?.Identity?.IsAuthenticated != true || roles.Length == 0)
        {
            return false;
        }

        foreach (var role in roles)
        {
            if (user.IsInRole(role))
            {
                return true;
            }
        }

        return false;
    }
}
