using Microsoft.Extensions.Configuration;
using Odin.Core.Config;

namespace Odin.Core.Organizations;

public interface IOrganizationConfigService
{
	OrganizationConfig Get(string organization);
}

public class OrganizationConfigService : IOrganizationConfigService
{
	private readonly Dictionary<string, OrganizationConfig> _organizationsConfig;

	public OrganizationConfigService(IConfiguration config)
	{
		_organizationsConfig = config.GetSection("organizations")
			.GetDynamic<Dictionary<string, OrganizationConfig>>() ?? new Dictionary<string, OrganizationConfig>();
	}

	public OrganizationConfig Get(string organization)
	{
		if (organization == null) throw new ArgumentNullException(nameof(organization));

		if (!_organizationsConfig.TryGetValue(organization, out var config))
			throw new KeyNotFoundException($"Organization not found '{organization}'.");
		return config;
	}
}
