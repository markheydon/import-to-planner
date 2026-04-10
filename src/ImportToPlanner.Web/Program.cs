using ImportToPlanner.Web.Components;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Infrastructure.Graph;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

builder.Services.AddScoped<ICsvImportParser, CsvImportParser>();
builder.Services.AddScoped<IImportPlannerOrchestrator, ImportPlannerOrchestrator>();
builder.Services.AddScoped<IPlannerGateway, InMemoryPlannerGateway>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
