var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Orleans_Messaging_Tests_Memory_SiloHost>("memory-silo");

builder.Build().Run();
