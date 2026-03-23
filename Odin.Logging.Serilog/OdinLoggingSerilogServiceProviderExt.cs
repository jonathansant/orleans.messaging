using Odin.Core.Logging;
using Odin.Logging.Serilog;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class OdinLoggingSerilogServiceCollectionExtensions
{
	public static IServiceCollection AddOdinLoggingSerilog(this IServiceCollection services)
	{
		services.AddScoped<ILoggingContext, SerilogLoggingContext>();

		return services;
	}
}
