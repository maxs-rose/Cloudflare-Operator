using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var installCrds = builder.AddExecutable("install-crds", "dotnet", builder.AppHostDirectory, "kubeops",  "install", new CloudflareOperator().ProjectPath);

var cfOperator = builder.AddProject<CloudflareOperator>("opeartor")
    .WaitForCompletion(installCrds);

installCrds.WithParentRelationship(cfOperator);

builder.Build().Run();