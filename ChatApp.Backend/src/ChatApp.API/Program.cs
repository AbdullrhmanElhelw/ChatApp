using ChatApp.API.Middlewares;
using ChatApp.API.Services;
using ChatApp.Application;
using ChatApp.Application.Common.Interfaces;
using ChatApp.Infrastructure;
using ChatApp.Infrastructure.Persistence;
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

// 5. Register Controllers
builder.Services.AddControllers();

// 6. Register CORS
const string corsPolicy = "ChatAppCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Mobile clients and local development
              .AllowAnyHeader()
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

app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
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
