using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Odin.Core.App;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class AppInfoCollectionExtensions
{
	public static IAppInfo AddAppInfo(this IServiceCollection services, IConfiguration config, IAppInfo? appInfo = null)
	{
		appInfo ??= new AppInfo(config);
		services.TryAddSingleton(appInfo);
		return appInfo;
	}
}
