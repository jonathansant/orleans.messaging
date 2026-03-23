using Odin.Core.Auth.Options;

namespace Odin.Core.Http;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class HttpClientConfig
{
	private string DebuggerDisplay => $"BaseUrl: '{Endpoint}', Identity: '{Identity}'";

	public string? BrandId { get; set; }
	public EndpointConfig Endpoint { get; set; } = new();
	public IdentityAuthConfig Identity { get; set; }
	public string AuthIdentityConfigKey { get; set; } = "identity";

	/// <summary>
	/// Ensure configuration is valid or throws.
	/// </summary>
	public void EnsureValid() =>
		Endpoint.Origin.IfNullOrEmptyThen(() => throw new ArgumentException($"{nameof(Endpoint.Origin)} must be provided."));
}
