using Odin.Core.Auth;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

public static class AuthActionContextExtensions
{
	/// <summary>
	/// Ensures the identity is an api consumer.
	/// </summary>
	/// <param name="action"></param>
	public static void EnsureApiConsumer(this IUserActionContext action)
		=> action.EnsureAuthenticated(AuthClaimTypes.ApiConsumer);

	/// <summary>
	/// Ensures the identity is a platform provider.
	/// </summary>
	/// <param name="action"></param>
	public static void EnsurePlatformProvider(this IUserActionContext action)
		=> action.EnsureAuthenticated(AuthClaimTypes.PlatformProvider);
}
