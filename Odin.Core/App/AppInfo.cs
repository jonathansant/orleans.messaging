using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Odin.Core.Config;
using Odin.Core.Versioning;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Odin.Core.App;

public interface IAppInfo
{
	/// <summary>
	/// Get application name. e.g. 'midgard.skeleton'.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the application short name. e.g. 'skeleton'.
	/// </summary>
	string ShortName { get; }

	/// <summary>
	/// Gets the application full name '{Name}-{Version}-{Environment}'. e.g. 'midgard.skeleton-1.10.16-dev'.
	/// </summary>
	string FullName { get; set; }

	/// <summary>
	/// Gets the application cluster group e.g. 'v1'. This is to have multiple clusters running independently e.g. v1 and v2.
	/// </summary>
	string ClusterGroup { get; }

	/// <summary>
	/// Gets the infrastructure cluster id e.g. 'nightking'.
	/// </summary>
	string InfraClusterId { get; }

	/// <summary>
	/// Gets the application cluster id, this will change across deployments '{Name}-{ClusterGroup}-{Version}-{Environment}'. e.g. 'midgard.skeleton-v1-1.10.16-dev'.
	/// </summary>
	string ClusterId { get; }

	/// <summary>
	/// Gets the application service id (or app id) '{Name}-{ClusterGroup}-{Environment}-v{MajorVersion}.{MinorVersion}'. e.g. 'midgard.skeleton-v1-dev.v1.6'.
	/// </summary>
	string ServiceId { get; }

	/// <summary>
	/// Gets the application ClusteredName '{InfraClusterId}.{Name}-{ClusterGroup}-{Environment}'. e.g. 'rig.midgard.skeleton-v1-dev'.
	/// </summary>
	string ClusteredName { get; }

	/// <summary>
	/// Gets the application product name. e.g. 'midgard'.
	/// </summary>
	string ProductName { get; }

	/// <summary>
	/// Get environment. e.g. 'Development'. (based on ASPNET_ENVIRONMENT, which can be mapped).
	/// </summary>
	string Environment { get; }

	/// <summary>
	/// Get git short commit hash. e.g. 'b603d6'
	/// </summary>
	string GitCommit { get; }

	/// <summary>
	/// Get application version. e.g. '1.1.0-rc.2'
	/// </summary>
	string Version { get; }

	[IgnoreDataMember]
	SemanticVersion SemanticVersion { get; }

	/// <summary>
	/// Get whether the app is dockerized or not.
	/// </summary>
	bool IsDockerized { get; }

	/// <summary>
	/// Gets which service type is this app responsible of e.g. web, silo, etc...
	/// </summary>
	string ServiceType { get; }

	/// <summary>
	/// Gets whether its in development mode or not (based on environment variable).
	/// </summary>
	bool IsDevelopment { get; }

	/// <summary>
	/// Gets whether its in staging mode or not (based on environment variable).
	/// </summary>
	bool IsStaging { get; }

	/// <summary>
	/// Gets whether its in production mode or not (based on environment variable).
	/// </summary>
	bool IsProduction { get; }

	/// <summary>
	/// Gets the additional props.
	/// </summary>
	Dictionary<string, object> Props { get; }

	int ProcessorCount { get; }

	string MachineName { get; }
}

public class AppInfo : IAppInfo
{
	private const string LocalEnvShortName = "local";
	private const string StagingEnvShortName = "staging";
	private const string ProdEnvShortName = "prod";
	private const string DefaultVersion = "0.0";

	public string Name { get; set; }
	public string ShortName { get; }
	public string ProductName { get; }
	public string ClusterGroup { get; }
	public string InfraClusterId { get; }
	public string ClusteredName { get; }
	public string ClusterId { get; }
	public string ServiceId { get; }

	[IgnoreDataMember]
	public string FullName { get; set; }

	[IgnoreDataMember]
	public string OriginalEnvironment { get; set; }

	public string Environment { get; set; }
	public string GitCommit { get; set; }
	public string Version { get; set; }
	public SemanticVersion SemanticVersion { get; }

	public string ServiceType { get; set; }

	[IgnoreDataMember]
	public bool IsDockerized { get; set; }

	[IgnoreDataMember]
	public bool IsDevelopment => Environment == LocalEnvShortName;

	[IgnoreDataMember]
	public bool IsStaging => Environment == StagingEnvShortName;

	[IgnoreDataMember]
	public bool IsProduction => Environment == ProdEnvShortName;

	public int ProcessorCount { get; }
	public int ClusterSize { get; }

	public string MachineName => System.Environment.MachineName;

	private static readonly Dictionary<string, string> EnvironmentMapping = new(StringComparer.OrdinalIgnoreCase)
	{
		["Development"] = "local",
		["Dev"] = "dev",
		["Staging"] = StagingEnvShortName,
		["Production"] = ProdEnvShortName,
		["Sandbox"] = "sandbox",
		["Test"] = "test",
	};

	public Dictionary<string, object> Props { get; } = new();

	private readonly Dictionary<string, object> _configTemplateMapping;

	public AppInfo()
	{
	}

	/// <summary>
	/// Resolve from <see cref="IConfiguration"/>.
	/// </summary>
	/// <param name="config"></param>
	public AppInfo(IConfiguration config)
	{
		Name = config.GetValue("APP_NAME", "product.app");
		ShortName = config.GetValue("APP_SHORT_NAME", string.Empty);
		Version = config.GetValue("APP_VERSION", DefaultVersion);
		GitCommit = config.GetValue("GIT_COMMIT", "-");
		ClusterGroup = config.GetValue("CLUSTER_GROUP", "");
		InfraClusterId = config.GetValue("CLUSTER_ID", "");
		OriginalEnvironment = config.GetValue<string>("ASPNETCORE_ENVIRONMENT")!;
		IsDockerized = config.GetValue<bool>("DOCKER");
		ServiceType = config.GetValue("serviceType", "dotnet");
		ProcessorCount = System.Environment.ProcessorCount;
		ClusterSize = (config.GetSection("cluster").Get<ClusterConfig>() ?? new ClusterConfig()).Size;

		if (Version == DefaultVersion)
		{
			var packageJsonFilePath = Path.Join(Directory.GetCurrentDirectory(), "package.json");
			var pkgJson = JsonNetExtensions.ReadFromFile(packageJsonFilePath);
			var pkgVersion = pkgJson?.GetValue("version")?.Value<string>();
			if (!pkgVersion.IsNullOrEmpty())
				Version = pkgVersion;
		}

		SemanticVersion = new(Version);

		var nameParts = Name.Split('.');
		ProductName = nameParts.First();
		ShortName = ShortName.IfNullOrEmptyReturn(() => nameParts.Length > 1 ? nameParts[1] : ProductName);

		OriginalEnvironment.IfNullOrEmptyThen(() => throw new InvalidOperationException("Environment is not set. Please specify the environment via 'ASPNETCORE_ENVIRONMENT'"));

		Environment = MapEnvironmentOrDefault(OriginalEnvironment);

		_configTemplateMapping = MapConfigDictionary();

		FullName = GetFullName(config);
		ClusteredName = GetClusterName(config);
		_configTemplateMapping["ClusterName"] = ClusteredName;

		ServiceId = GetServiceId(config);
		_configTemplateMapping[nameof(ServiceId)] = ServiceId;

		ClusterId = GetClusterId(config);
		_configTemplateMapping[nameof(ClusterId)] = ClusterId;

		var pkgVerMetadata = OdinCorePackageMeta.Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "PackageVersion");
		Props["odinCoreVersion"] = pkgVerMetadata?.Value ?? OdinCorePackageMeta.Version.ToString();
	}

	public static string MapEnvironmentOrDefault(string environment)
	{
		ArgumentException.ThrowIfNullOrEmpty(environment);

		return EnvironmentMapping.GetValueOrDefault(environment, environment);
	}

	public override string ToString()
	{
		var version = Version == DefaultVersion ? string.Empty : $" v{Version}";
		var infra = InfraClusterId.IsNullOrEmpty() ? string.Empty : $"[{InfraClusterId}] ";

		return $"{infra}{Name}{version} - {Environment}";
	}

	private Dictionary<string, object> MapConfigDictionary()
		=> new(StringComparer.OrdinalIgnoreCase)
		{
			["Name"] = Name,
			["Version"] = Version,
			["GitCommit"] = GitCommit,
			["ClusterGroup"] = ClusterGroup,
			["InfraClusterId"] = InfraClusterId,
			["Env"] = Environment,
			["ProductName"] = ProductName,
			["ShortName"] = ShortName,
			["MajorVersion"] = SemanticVersion.Version.Major.ToString(),
			["MinorVersion"] = SemanticVersion.Version.Minor.ToString(),
			["PatchVersion"] = SemanticVersion.Version.Build.ToString(),
			["PrereleaseVersion"] = SemanticVersion.SpecialVersion,
		};

	private string ParseConfigTemplate(IConfiguration config, string key, string defaultTemplate)
		=> config.GetValue(key, defaultTemplate)
			.FromTemplate(_configTemplateMapping)
			.Replace("--", "-")
			.Replace("..", ".")
			.Replace("-.", ".")
			.Trim('.', '-', '_')
	;

	private string GetFullName(IConfiguration config)
		=> ParseConfigTemplate(config, "APP_FULLNAME_TEMPLATE", "{Name}-{Version}-{Env}");

	private string GetClusterId(IConfiguration config)
		=> ParseConfigTemplate(config, "APP_CLUSTERID_TEMPLATE", "{ServiceId}.{PatchVersion}-{PrereleaseVersion}");

	private string GetServiceId(IConfiguration config)
		=> ParseConfigTemplate(config, "APP_SERVICEID_TEMPLATE", "{ClusterName}.v{MajorVersion}.{MinorVersion}");

	private string GetClusterName(IConfiguration config)
		=> ParseConfigTemplate(config, "APP_CLUSTERNAME_TEMPLATE", "{InfraClusterId}.{Name}-{ClusterGroup}-{Env}");
}
