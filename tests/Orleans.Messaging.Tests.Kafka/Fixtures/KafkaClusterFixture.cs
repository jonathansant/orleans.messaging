using Projects;

namespace Orleans.Messaging.Tests.Kafka.Fixtures;

public class KafkaClusterFixture : IAsyncLifetime
{
	private DistributedApplication? _app;
	private IHost? _clientHost;

	public IServiceProvider ServiceProvider => _clientHost!.Services;
	public IGrainFactory GrainFactory => _clientHost!.Services.GetRequiredService<IGrainFactory>();

	public async Task InitializeAsync()
	{
		// Start the Aspire AppHost (launches Kafka container + KafkaUI + Orleans silo)
		var appBuilder = await DistributedApplicationTestingBuilder
			.CreateAsync<Orleans_Messaging_KafkaAppHost>();

		_app = await appBuilder.BuildAsync();
		await _app.StartAsync();

		// Retrieve Kafka bootstrap server from Aspire
		var bootstrapServer = await _app.GetConnectionStringAsync("kafka")
							?? throw new InvalidOperationException("Kafka connection string not available from Aspire.");

		// Wait for the silo to be ready and the ConsumerGrainService to position offsets
		// at the end of the topic before any test produces messages (ConsumeMode.Last).
#if DEBUG
		await Task.Delay(TimeSpan.FromSeconds(15));
#else
		await Task.Delay(TimeSpan.FromSeconds(5));
#endif

		_clientHost = Host.CreateDefaultBuilder()
			.UseOrleansClient(c => c.UseLocalhostClustering(30001))
			.AddMessagingKafkaClient(MessageBrokerNames.DefaultBroker, _ => { })
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
