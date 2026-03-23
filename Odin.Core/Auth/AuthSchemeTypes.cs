namespace Odin.Core.Auth;

public static class AuthSchemeTypes
{
	public const string BearerScheme = "Bearer";
	public const string ApiSecretScheme = "Secret";
	public const string BasicScheme = "Basic";
}

public static class AuthClaimTypes
{
	/// <summary>
	/// Claim which specify which source id will be used for config.
	/// </summary>
	public const string ConfigSourceId = "configSourceId";

	/// <summary>
	/// Claim which identifies identity has access as an api consumer, such as a frontend server.
	/// </summary>
	public const string ApiConsumer = "api-consumer";

	/// <summary>
	/// Claim which identifies identity has access to platform specific writes e.g. pushing data to api.
	/// </summary>
	public const string PlatformProvider = "api-platform-provider";
}
