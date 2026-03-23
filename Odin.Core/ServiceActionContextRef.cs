using Odin.Core.Auth;
using Odin.Core.DeviceDetection;
using Odin.Core.Http;
using Odin.Core.Json;

namespace Odin.Core;

public interface IServiceActionContext : IActionContext
{
	/// <summary>
	/// Gets the token for a particular token client e.g. midgard-oauth, paymentIQ-oauth
	/// </summary>
	/// <param name="clientId"></param>
	string? GetEgressToken(string? clientId);
	ApiProviderModel? IngressIdentity { get; }
	HashSet<string>? Tags { get; set; }
	string? UserId { get; set; }
	string? Username { get; set; }

	new ServiceActionRef ToRef();
	OdinActionRef IActionContext.ToRef()
		=> throw new NotImplementedException();
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record ServiceActionRef : IServiceActionContext
{
	protected virtual string DebuggerDisplay => $"CorrelationId: '{CorrelationId}', UserId: '{UserId}', MockResponse: '{MockResponse}', Tags: {Tags.ToDebugString()}";

	public ServiceActionRef(ServiceContext serviceContext)
	{
		ServiceContext = serviceContext;
	}

	[Id(0)]
	[HashIgnore]
	public string CorrelationId { get; set; } = null!;

	[Id(1)]
	public string? UserId { get; set; }
	[Id(2)]
	public HashSet<string>? Tags { get; set; }
	[Id(3)]
	public string? Username { get; set; }
	// this need to be public for orleans serialization
	[Id(4)]
	[HashIgnore]
	public ServiceContext ServiceContext { get; set; }
	[Id(5)]
	[HashIgnore]
	public string? MockResponse { get; set; }
	[Id(6)]
	public DeviceType DeviceType { get; set; }

	public ApiProviderModel? IngressIdentity => ServiceContext.IngressIdentity;

	public void EnsureAuthenticated(string? claim = null, string? allowClaimValue = null)
	{
		if (claim.IsNullOrEmpty())
			return;

		if (IngressIdentity?.Claims.TryGetValue(claim, out var claimValue) != true)
			throw UnauthorizedException.ClaimRequired(claim);
		if (allowClaimValue != null && allowClaimValue != claimValue)
			throw UnauthorizedException.ClaimRequired(claim, allowClaimValue);
	}

	public string? GetEgressToken(string? clientId)
	{
		if (clientId.IsNullOrEmpty()) return null;
		ServiceContext.EgressClientTokens.TryGetValue(clientId, out var token);
		return token;
	}

	/// <summary>
	/// User-defined conversion from <see cref="ServiceActionRef"/> to <see cref="HttpRequestClientContext"/>.
	/// </summary>
	/// <param name="action"></param>
	public static implicit operator HttpRequestClientContext(ServiceActionRef action)
		=> action.ToHttpRequestClientContext();

	public ServiceActionRef ToRef()
		=> this;
}

// todo: rename to ServiceIdentityState or so
[GenerateSerializer]
public record ServiceContext
{
	[Id(0)]
	[HashIgnore]
	public ApiProviderModel? IngressIdentity { get; set; }
	[Id(1)]
	[HashIgnore]
	public IDictionary<string, string> EgressClientTokens { get; set; } = null!;
}
