using Orleans.Messaging;
using Orleans.Messaging.Memory.Config;

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleans(silo =>
{
	silo.UseLocalhostClustering(siloPort: 11111, gatewayPort: 30000);
	silo.AddMemoryGrainStorage("messaging");

	// new MessagingMemoryBuilder(silo, MessageBrokerNames.DefaultBroker)
	// 	.WithStoreName("messaging")
	// 	.Build();
});

await builder.Build().RunAsync();

public partial class Program { }
