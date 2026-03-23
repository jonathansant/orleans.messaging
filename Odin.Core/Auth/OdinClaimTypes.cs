using System.Security.Claims;

namespace Odin.Core.Auth;

public class OdinClaimTypes
{
	public const string Permission = "permission";
	public const string Organization = "organization";
	public const string Brand = "brand";
	public const string Role = ClaimTypes.Role;
	public const string Scope = "scope";
}

public class OdinRoleValues
{
	public const string SuperAdmin = "Super Admin";
}
