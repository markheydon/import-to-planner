#:sdk Aspire.AppHost.Sdk@13.2.2

#pragma warning disable ASPIRECSHARPAPPS001
var builder = DistributedApplication.CreateBuilder(args);

builder.AddCSharpApp("web", "./src/ImportToPlanner.Web/ImportToPlanner.Web.csproj", options =>
{
	options.LaunchProfileName = "https";
});

builder.Build().Run();
