namespace Odin.Core.Auth;

/// <summary>
/// Exception is thrown when trying to access an authenticated function without being authenticated.
/// </summary>
[GenerateSerializer]
public class UnauthorizedException : Exception
{
	[Id(0)]
	public HashSet<string>? Claims { get; set; }
	[Id(1)]
	public string? ErrorCode { get; set; } = OdinErrorCodes.Auth.Unauthorized;

	public bool IsPermissionDenied => ErrorCode is OdinErrorCodes.Auth.PermissionDenied
		or OdinErrorCodes.Auth.ClaimRequired
		or OdinErrorCodes.Auth.ClaimRequiredValue
	;

	public UnauthorizedException()
	{
	}

	public UnauthorizedException(string message) : base(message)
	{
	}

	public UnauthorizedException(string message, Exception? inner = null, string? errorCode = null)
		: base(message, inner)
	{
		ErrorCode = errorCode;
	}

	public static UnauthorizedException ClaimRequired(string claim)
		=> new($"No claim '{claim}' provided.")
		{
			Claims = claim.ToSingleHashSet(),
			ErrorCode = OdinErrorCodes.Auth.ClaimRequired
		};

	public static UnauthorizedException ClaimRequired(string claim, string claimValue)
		=> new($"No claim '{claim}={claimValue}' provided.")
		{
			Claims = $"{claim}={claimValue}".ToSingleHashSet(),
			ErrorCode = OdinErrorCodes.Auth.ClaimRequiredValue
		};

	public static UnauthorizedException PermissionDenied(string? permission = null)
		=> PermissionsDenied(permission?.ToSingleHashSet());

	public static UnauthorizedException PermissionsDenied(HashSet<string>? permissions = null)
	{
		var claims = permissions ?? new HashSet<string>();
		return new(claims.IsNullOrEmpty()
			? "Permission denied."
			: $"Permission denied for permission '{string.Join(",", claims)}'"
		)
		{
			Claims = claims,
			ErrorCode = OdinErrorCodes.Auth.PermissionDenied
		};
	}
}
