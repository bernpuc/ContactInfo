using ContactInfo.Components;
using ContactInfo.Models;
using ContactInfo.Services;
using ContactInfo.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(1));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton<IUserSettingsService, UserSettingsService>();
builder.Services.AddSingleton<IExcelService, ExcelService>();
builder.Services.AddScoped<BatchProcessorService>();

// Register each lookup source — add more here as new providers are integrated
// builder.Services.AddHttpClient<IContactSource, ApolloService>();   // re-enable with a paid Apollo plan

// Demo sources — remove when real providers are configured
builder.Services.AddSingleton<IContactSource>(new DemoContactSource("DemoA", variant: 1));
builder.Services.AddSingleton<IContactSource>(new DemoContactSource("DemoB", variant: 2));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
