using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Text.Json;
using System.Text.Json.Serialization;
using AerialWatch.Api.Authentication;
using AerialWatch.Api.ErrorHandling;
using AerialWatch.Api.Logging;
using AerialWatch.Api.Routing;
using AerialWatch.Application;
using AerialWatch.Infrastructure;
using Serilog;

// Web host and composition root. Controllers depend only on AerialWatch.Contracts interfaces;
// the implementations from Application and Infrastructure are registered here.
var builder = WebApplication.CreateBuilder(args);

// Logs: warnings and errors to SQL core.ErrorLogs + daily file fallback (D-036, Api/Logging/SerilogSetup.cs).
builder.Host.UseSerilog((context, logger) => SerilogSetup.Configure(logger, context.Configuration, context.HostingEnvironment));

builder.Services
    .AddControllers(options =>
    {
        options.Conventions.Add(new ModuleRouteConvention());
        options.Conventions.Add(new RouteTokenTransformerConvention(new CamelCaseRouteTransformer()));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ModelStateResponses.Friendly);

// The bypass cannot activate outside Development, even if configuration requests it.
var developmentBypass = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Authentication:DevelopmentBypass");
var authScheme = developmentBypass ? DevelopmentAuthenticationHandler.SchemeName : CookieAuthenticationDefaults.AuthenticationScheme;
var authentication = builder.Services.AddAuthentication(authScheme);
if (developmentBypass)
    authentication.AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(authScheme, _ => { });
else
    authentication.AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

builder.Services.AddApplication();                          // business logic (services), per module
builder.Services.AddInfrastructure(builder.Configuration);  // SQL Server, repositories, clock

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string FrontendCors = "frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options => options.AddPolicy(FrontendCors, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // http://localhost:5080/swagger
}

app.UseCors(FrontendCors);
app.UseAuthentication();
app.UseMiddleware<RequestLogContext>(); // module, feature, user, path, trace id on every log line
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

// Exposed for integration tests (WebApplicationFactory).
public partial class Program;
