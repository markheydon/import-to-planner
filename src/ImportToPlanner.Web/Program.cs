using ImportToPlanner.Web;

var builder = ImportToPlannerWebHost.CreateWebApplicationBuilder(args);
var app = builder.Build();
ImportToPlannerWebHost.ConfigureWebApplication(app);
app.Run();
