using ChatApp.API.Hubs;
using ChatApp.API.Middlewares;
using ChatApp.API.Services;
using ChatApp.Application;
using ChatApp.Application.Common.Interfaces;
using ChatApp.Infrastructure;
using ChatApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Structured Logging with Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

// 2. Register Application & Infrastructure Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Register API Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 4. Register Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready", "db" });

// 5. Register Controllers & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();

// 6. Register CORS
const string corsPolicy = "ChatAppCorsPolicy";
var allowedCorsOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        if (allowedCorsOrigins.Length > 0)
        {
            policy.WithOrigins(allowedCorsOrigins);
        }

        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// 7. Register Swagger with JWT Bearer Definition
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatApp API",
        Version = "v1",
        Description = "Enterprise Messaging Platform Backend API (.NET 10)"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Apply pending database migrations before seeding or accepting requests.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await context.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to apply database migrations on startup.");
        throw;
    }

    try
    {
        await AppDbContextSeed.SeedAsync(context);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to seed database on startup.");
    }
}

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatApp API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting ChatApp Web API on .NET 10...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ChatApp Web API terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program public for integration testing with WebApplicationFactory
public partial class Program { }
