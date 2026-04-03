using Orleans.Messaging;
using Orleans.Messaging.Kafka.Config;
using Orleans.Messaging.Kafka.Consuming;
using Orleans.Messaging.Kafka.Serialization;
using Orleans.Messaging.Tests.Grains;

var builder = Host.CreateApplicationBuilder(args);

var bootstrapServer = builder.Configuration.GetConnectionString("kafka") ?? "localhost:9092";

var siloPort = builder.Configuration.GetValue<int>("Orleans:SiloPort", 11111);
var gatewayPort = builder.Configuration.GetValue<int>("Orleans:GatewayPort", 30000);
var primarySiloPort = builder.Configuration.GetValue<int?>("Orleans:PrimarySiloPort", null);
var primarySiloEndpoint = primarySiloPort.HasValue
	? new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, primarySiloPort.Value)
	: null;

builder.UseOrleans(silo =>
	{
		silo.UseLocalhostClustering(siloPort, gatewayPort, primarySiloEndpoint);
		silo.AddMemoryGrainStorage("messaging");
		// silo.AddStartupTask<PlaygroundActivate>();
		silo.AddStartupTask(
			async (sp, _) =>
			{
				var client = sp.GetRequiredService<IConsumerGrainServiceClient>();
				await client.InitializeConsumersOnAllSilos();
			},
			GrainLifecycleStage.Last
		);

		silo.AddMessagingKafka(
				MessageBrokerNames.DefaultBroker,
				cfg => cfg.WithOptions(opts =>
					{
						opts.StoreName = "messaging";
						opts.BrokerList = [bootstrapServer];
						opts.ConsumerGroupId = "test-silo-group";
						opts.ConsumeMode = ConsumeMode.Last;
						opts.PollRate = TimeSpan.FromMilliseconds(50);
						opts.BatchSize = 10;
					}
				)
			)
			;
	}
);

var host = builder.Build();
host.Services.ConfigureMessagingKafka(
	MessageBrokerNames.DefaultBroker,
	(sp, _) => sp.AddTopic(
		"test-messages",
		MessageBrokerNames.DefaultBroker,
		topic => topic
			.WithContract<TestMessage>()
			.WithCreationOptions(
				new()
				{
					AutoCreate = true,
					Partitions = 5,
					ReplicationFactor = 1
				}
			)
			.WithPollRate(TimeSpan.FromMilliseconds(50))
			.WithBatchSize(10)
			.WithTopicType(TopicType.InOut)
			.WithJsonSerializer()
	)
);

await host.RunAsync();
