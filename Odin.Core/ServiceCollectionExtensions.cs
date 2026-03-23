using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Odin.Core;
using Odin.Core.App;
using Odin.Core.Auth;
using Odin.Core.Auth.Permissions;
using Odin.Core.Caching;
using Odin.Core.Config;
using Odin.Core.Config.MultiCluster;
using Odin.Core.Countries;
using Odin.Core.Error;
using Odin.Core.FlowControl;
using Odin.Core.Http;
using Odin.Core.Http.Lifecycle;
using Odin.Core.Json;
using Odin.Core.Key;
using Odin.Core.Organizations;
using Odin.Core.Serialization;
using Odin.Core.User;
using Odin.Core.Utils;
using System.Net;
using RequestContext = Odin.Core.Http.Lifecycle.RequestContext;

namespace Odin.Core
{
	public class CoreServiceCollectionResult
	{
		public IAppInfo AppInfo { get; set; }
	}
}

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
	public static class OdinCoreServiceCollectionExtensions
	{
		public static CoreServiceCollectionResult AddOdinCore(
			this IServiceCollection services,
			IConfiguration config,
			IAppInfo? appInfo = null
		)
		{
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
			}.WithOdinDataTypeConvertors();

			services.AddSingleton(config);
			services.AddSingleton<ITokenParser, OdinTokenParser>();
			services.AddScoped<ITokenManager, TokenManager>();
			services.AddSingleton<IAsymmetricTokenService, RsaAsymmetricTokenService>();

			services.AddScoped<IUserContext, UserContext>();
			services.TryAddSingleton<IApiProviderService, ApiProviderService>();
			services.TryAddSingleton<IConfigBuilderFactory, ConfigBuilderFactory>();
			services.TryAddSingleton<IConfigBuilder, ConfigBuilder>();
			services.TryAddSingleton<IConfigMerger, ConfigMerger>();
			services.TryAddSingleton<IOrganizationConfigService, OrganizationConfigService>();
			services.TryAddSingleton<ICountryService, CountryService>();
			services.TryAddSingleton<IErrorExplorerLocator, ErrorExplorerLocator>();
			services.TryAddSingleton(typeof(IKeyBuilderFactory<>), typeof(KeyBuilderFactory<>));
			services.TryAddSingleton<IScheduledThrottledActionFactory, ScheduledThrottledActionFactory>();
			services.TryAddSingleton<IThrottledActionFactory, ThrottledActionFactory>();
			services.TryAddSingleton<IStringTokenParserFactory, StringTokenParserFactory>();
			services.TryAddSingleton<IMultiClusterConfigService, MultiClusterConfigService>();
			services.TryAddSingleton<IMultiClusterClientResolver, MultiClusterClientResolver>();
			services.AddOdinCaching();
			services.AddFluentlyHttpClient(builder => builder
				.WithAutoRegisterFactory()
				.WithRequestBuilderDefaults(requestBuilder =>
					{
						requestBuilder.WithVersion(HttpVersion.Version11);
					}
				)
			);
			services.AddScoped<RequestContext>(); // todo: remove this and register directly from interface (tho need to remove usages)
			services.AddFromExisting<IRequestContext, RequestContext>();

			var fluentConfig = config.GetSection("fluentlyHttpClientEntity").Get<FluentlyHttpClientEntityConfig>()
							   ?? new FluentlyHttpClientEntityConfig();
			services.AddSingleton(fluentConfig);

			services.Configure<List<ApiProviderModel>>(config.GetSection("apiProviders"));
			services.RemoveAll<IHttpMessageHandlerBuilderFilter>(); // remove logging message handler from HttpClient

			appInfo = services.AddAppInfo(config, appInfo);

			services.TryAddSingleton<IPermissionLocator, PermissionLocator>();
			services.AddOdinSerialization();

			return new()
			{
				AppInfo = appInfo
			};
		}
	}
}
