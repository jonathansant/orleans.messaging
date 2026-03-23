using FluentlyHttpClient;

namespace Odin.Core.Http;

public abstract class MultiClusterHttpClient
{
	protected IFluentHttpClient HttpClient { get; private set; }
	public abstract string ResourceName { get; }
	protected virtual string BaseUrl { get; set; } = "api/v1";

	protected MultiClusterHttpClient(
		IFluentHttpClient httpClient
	)
	{
		// ReSharper disable once VirtualMemberCallInConstructor
		HttpClient = InitializeClient(httpClient);
	}

	protected virtual IFluentHttpClient InitializeClient(IFluentHttpClient fluentHttpClient)
		=> fluentHttpClient
			.CreateClient(ResourceName)
			.WithBaseUrl(BaseUrl, replace: false)
			.With(ConfigureClient)
			.Build();

	protected virtual void ConfigureClient(FluentHttpClientBuilder builder)
	{ }
}
