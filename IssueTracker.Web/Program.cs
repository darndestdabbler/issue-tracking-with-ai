using IssueTracker.Web.Components;
using IssueTracker.Web.Data;
using IssueTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddMudServices();
builder.Services.AddScoped<PostService>();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddHttpClient("IssueTrackerApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5124");
});
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("IssueTrackerApi"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Issue Tracker API", Version = "v1" });
});

// Register EF Core with switchable SQLite / SQL Server provider
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SQLite";
if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
}
else
{
    var dbPath = Environment.GetEnvironmentVariable("ISSUETRACKER_DB_PATH");
    var connectionString = dbPath != null
        ? $"Data Source={dbPath}"
        : builder.Configuration.GetConnectionString("SQLite");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

var app = builder.Build();

// Migrate database and seed reference data on startup
await DatabaseSeeder.SeedAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Issue Tracker API v1");
    c.RoutePrefix = "swagger";
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
