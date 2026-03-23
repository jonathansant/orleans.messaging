using Odin.Core.App;

namespace Odin.Orleans.Core;

public static class AppInfoExtensions
{
	public const string SiloServiceType = "silo";

	public static bool IsSilo(this IAppInfo appInfo)
		=> appInfo.ServiceType == SiloServiceType;
}
