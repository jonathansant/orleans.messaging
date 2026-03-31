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

builder.AddProject<Projects.Orleans_Messaging_Tests_Kafka_SiloHost>("kafka-silo")
	.WithReference(kafka)
	.WaitFor(kafka);

builder.Build().Run();