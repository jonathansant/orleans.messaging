var builder = DistributedApplication.CreateBuilder(args);

var silo1 = builder.AddProject<Projects.Orleans_Messaging_Tests_Memory_SiloHost>("memory-silo-1")
	.WithEnvironment("Orleans__SiloPort", "11111")
	.WithEnvironment("Orleans__GatewayPort", "30000");

builder.AddProject<Projects.Orleans_Messaging_Tests_Memory_SiloHost>("memory-silo-2")
	.WaitFor(silo1)
	.WithEnvironment("Orleans__SiloPort", "11112")
	.WithEnvironment("Orleans__GatewayPort", "30001")
	.WithEnvironment("Orleans__PrimarySiloPort", "11111");

builder.Build().Run();
