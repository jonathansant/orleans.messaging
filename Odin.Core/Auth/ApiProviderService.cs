using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Odin.Core.Auth;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public class ApiProviderModel
{
	protected string DebuggerDisplay => $"Id: '{Id}', Name: '{Name}', ApiKey: '{ApiKey}'";

	[Id(0)]
	public string Id { get; set; }

	[Id(1)]
	public string Name { get; set; }

	[Id(2)]
	public string ApiKey { get; set; }

	[Id(3)]
	public Dictionary<string, string> Claims { get; set; }

	public IEnumerable<Claim> ToSecurityClaims()
		=> Claims.IsNullOrEmpty() ? new List<Claim>() : Claims.Select(x => new Claim(x.Key, x.Value));

	public override string ToString() => DebuggerDisplay;
}

public interface IApiProviderService
{
	Task<ApiProviderModel?> GetByKey(string key);
	Task<ApiProviderModel?> GetById(string id);
}

public class ApiProviderService : IApiProviderService
{
	private readonly List<ApiProviderModel> _apiProviders;

	public ApiProviderService(IOptions<List<ApiProviderModel>> options)
	{
		_apiProviders = options.Value ?? [];
	}

	public Task<ApiProviderModel?> GetByKey(string key)
		=> Task.FromResult(_apiProviders.Find(x => x.ApiKey == key));

	public Task<ApiProviderModel?> GetById(string id)
		=> Task.FromResult(_apiProviders.Find(x => x.Id == id));
}
