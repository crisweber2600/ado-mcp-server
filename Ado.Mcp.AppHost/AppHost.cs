var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Ado_Mcp>("ado-mcp");

builder.Build().Run();
