using System.Net;
using Orleans.Messaging;
using Orleans.Messaging.Memory.Config;

var builder = Host.CreateApplicationBuilder(args);

var siloPort = builder.Configuration.GetValue<int>("Orleans:SiloPort", 11111);
var gatewayPort = builder.Configuration.GetValue<int>("Orleans:GatewayPort", 30000);
var primarySiloPort = builder.Configuration.GetValue<int?>("Orleans:PrimarySiloPort", null);
var primarySiloEndpoint = primarySiloPort.HasValue
	? new IPEndPoint(IPAddress.Loopback, primarySiloPort.Value)
	: null;

builder.UseOrleans(silo =>
	{
		silo.UseLocalhostClustering(siloPort, gatewayPort, primarySiloEndpoint);
		silo.AddMemoryGrainStorage("messaging");

		silo.AddMessagingMemory(
			MessageBrokerNames.DefaultBroker,
			cfg =>
			{
				cfg.WithStoreName("messaging")
					// .WithOptions(opts =>
					// {
					// 	opts.MaxPartitionCount = 5;
					// })
					;
			}
		);
	}
);

await builder.Build().RunAsync();
