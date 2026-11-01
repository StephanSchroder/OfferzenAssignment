using System.Text.Json.Serialization;
using Assignment.Api.Data;
using Assignment.Api.OData;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OData.UriParser;

var builder = WebApplication.CreateBuilder(args);

// --- Database (EF Core + SQLite, connection string from appsettings.json) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlite(connectionString)
    // Report (rather than crash on) a model that has drifted from the latest migration;
    // run `dotnet ef migrations add <Name>` when you see this warning.
    .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning)));

// --- Controllers + OData ---
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Count()
        .SetMaxTop(ODataQueryDefaults.MaxTop)
        .AddRouteComponents(
            EdmModelBuilder.RoutePrefix,
            EdmModelBuilder.Build(),
            services => services.AddSingleton<ODataUriResolver>(
                // Allows $filter=status eq 'Pending' (no namespace prefix) and case-insensitive names.
                new StringAsEnumResolver { EnableCaseInsensitive = true })));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Ensures the database exists and runs all pending migrations before serving requests.
await app.InitialiseDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();          // /openapi/v1.json
    app.UseODataRouteDebug();  // /$odata lists the OData routes
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(FrontendCorsPolicy);

app.MapControllers();

app.Run();
