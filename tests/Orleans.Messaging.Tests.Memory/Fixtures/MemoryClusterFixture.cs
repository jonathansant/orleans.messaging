using Projects;

namespace Orleans.Messaging.Tests.Memory.Fixtures;

public class MemoryClusterFixture : IAsyncLifetime
{
	private DistributedApplication? _app;
	private IHost? _clientHost;

	public IServiceProvider ServiceProvider => _clientHost!.Services;
	public IGrainFactory GrainFactory => _clientHost!.Services.GetRequiredService<IGrainFactory>();

	public async Task InitializeAsync()
	{
		var appBuilder = await DistributedApplicationTestingBuilder
			.CreateAsync<Orleans_Messaging_MemoryAppHost>();

		_app = await appBuilder.BuildAsync();
		await _app.StartAsync();

		// Wait for the silo to be ready before connecting
#if DEBUG
		await Task.Delay(TimeSpan.FromSeconds(15));
#else
		await Task.Delay(TimeSpan.FromSeconds(3));
#endif

		_clientHost = Host.CreateDefaultBuilder()
			.UseOrleansClient(c => c.UseLocalhostClustering(30000))
			.ConfigureServices(services =>
				{
					new MessagingMemoryBuilder(services, MessageBrokerNames.DefaultBroker)
						.WithStoreName("messaging")
						.Build();
				}
			)
			.Build();

		await _clientHost.StartAsync();
	}

	public async Task DisposeAsync()
	{
		if (_clientHost is not null)
		{
			await _clientHost.StopAsync();
			_clientHost.Dispose();
		}

		if (_app is not null)
		{
			await _app.StopAsync();
			await _app.DisposeAsync();
		}
	}
}
