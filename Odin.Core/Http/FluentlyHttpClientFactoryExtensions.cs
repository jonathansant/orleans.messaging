using FluentlyHttpClient;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FluentlyHttpClientFactoryExtensions
{
	public static IFluentHttpClientFactory GetFluentHttpClientFactory(this IServiceProvider serviceProvider)
		=> serviceProvider.GetRequiredService<IFluentHttpClientFactory>();
}
