using DocumentProcessor.WebForms.Components;
using DocumentProcessor.WebForms.Configuration;
using DocumentProcessor.WebForms.Data;
using DocumentProcessor.WebForms.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Strongly-typed options bound from appsettings.json sections
// ---------------------------------------------------------------------------
builder.Services.Configure<BedrockOptions>(builder.Configuration.GetSection("Bedrock"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

// ---------------------------------------------------------------------------
// Kestrel request-body size (was IIS maxRequestLength / maxAllowedContentLength)
// ---------------------------------------------------------------------------
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var maxBodySize = builder.Configuration.GetValue<long?>("Kestrel:Limits:MaxRequestBodySize");
    if (maxBodySize.HasValue)
    {
        serverOptions.Limits.MaxRequestBodySize = maxBodySize.Value;
    }
});

// ---------------------------------------------------------------------------
// Database — resolve connection string, register EF Core DbContext
// ---------------------------------------------------------------------------
var connectionString = DatabaseConnectionResolver.Resolve(builder.Configuration, builder.Services);

builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseSqlServer(connectionString.ConnectionString));

// DatabaseInfo singleton — injected by MainLayout.razor to show provider/host in the UI banner
builder.Services.AddSingleton(connectionString.Info);

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IDocumentStorage, FileSystemDocumentStorage>();
builder.Services.AddScoped<IDocumentSummarizer, BedrockDocumentSummarizer>();
builder.Services.AddScoped<DocumentTextExtractor>();
builder.Services.AddScoped<DocumentPipeline>();
builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// Blazor Server
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Database schema initialisation (was Application_Start → DatabaseInitializer.EnsureSchema)
// ---------------------------------------------------------------------------
try
{
    DatabaseInitializer.EnsureSchema(connectionString.ConnectionString, app.Environment);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Could not prepare the database.");
}

// ---------------------------------------------------------------------------
// Middleware pipeline — order is load-bearing
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/ErrorPage");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// ---------------------------------------------------------------------------
// Global error logging (was Application_Error)
// ---------------------------------------------------------------------------
// In development, the developer exception page is already active.
// For non-development, UseExceptionHandler above catches unhandled errors and
// routes to ErrorPage. Additionally, log every unhandled exception:
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Unhandled application error.");
            throw; // re-throw so the developer exception page still renders
        }
    });
}

// ---------------------------------------------------------------------------
// Root route redirect: Default.aspx was the default document; ensure "/" resolves
// once the Default page is ported to a Blazor component with @page "/".
// If no component owns "/", this redirect prevents a 404 at the site root.
// ---------------------------------------------------------------------------
// PORT-TODO
// {"note": "Default.aspx is being ported to a Blazor component. Verify that the ported component declares @page \"/\" so the site root resolves. If it uses a different route, add a redirect here: app.MapGet(\"/\", ctx => { ctx.Response.Redirect(\"/YourRoute\"); return Task.CompletedTask; });"}

// ---------------------------------------------------------------------------
// Blazor Server endpoint mapping (modern .NET 8+ pattern)
// ---------------------------------------------------------------------------
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
