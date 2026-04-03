var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder
		.AddKafka("kafka", 9090)
		// .WithEndpoint(port: 9092, targetPort: 50000, scheme: "tcp", name: "kafka")
		// .WithEndpoint(port: 9092, targetPort: 50000, scheme: "http", name: "kafkaHttp")
		// .WithEndpoint(port: 9090, targetPort: 9092, scheme: "tcp", name: "kafka")
		.PublishAsConnectionString()
		.WithDataVolume()
		.WithKafkaUI(cfg => cfg.WithHostPort(8082))
	;

var silo1 = builder.AddProject<Projects.Orleans_Messaging_Tests_Kafka_SiloHost>("kafka-silo-1")
	.WithReference(kafka)
	.WaitFor(kafka)
	.WithEnvironment("Orleans__SiloPort", "11111")
	.WithEnvironment("Orleans__GatewayPort", "30000");

builder.AddProject<Projects.Orleans_Messaging_Tests_Kafka_SiloHost>("kafka-silo-2")
	.WithReference(kafka)
	.WaitFor(kafka)
	.WaitFor(silo1)
	.WithEnvironment("Orleans__SiloPort", "11112")
	.WithEnvironment("Orleans__GatewayPort", "30001")
	.WithEnvironment("Orleans__PrimarySiloPort", "11111");

builder.Build().Run();