using Fantasy.ControlCenter.Api;
using Fantasy.ControlCenter.Components;
using Fantasy.ControlCenter.Controllers;
using Fantasy.ControlCenter.Infrastructure;
using Fantasy.ControlCenter.Options;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services
    .AddOptions<ControlCenterOptions>()
    .Bind(builder.Configuration.GetSection(ControlCenterOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<ControlCenterDatabase>();
builder.Services.AddSingleton<ControlCenterRepository>();
builder.Services.AddSingleton<ServiceRegistry>();
builder.Services.AddHostedService(services => services.GetRequiredService<ServiceRegistry>());
builder.Services.AddSingleton<ControlCenterStore>();
builder.Services.AddControllers(options =>
    options.Filters.Add(new ConfigurationExceptionFilter()));
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

await app.Services.GetRequiredService<ControlCenterStore>().InitializeAsync();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapControlCenterApi();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
