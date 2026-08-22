using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using SmartHome.Config;
using SmartHome.Data;
using SmartHome.Data.Repositories;
using SmartHome.Infrastructure;
using SmartHome.Infrastructure.Hubs;
using SmartHome.Middlewares;
using SmartHome.Services;
using SmartHome.Settings;

var builder = WebApplication.CreateBuilder(args);

#region WebSockets SignalR Configs

builder.Services.AddSingleton<
    Microsoft.AspNetCore.SignalR.IUserIdProvider,
    MonitexUserIdProvider>();

builder.Services.AddSignalR();

#endregion

#region Kestrel Port Configuration

builder.WebHost.ConfigureKestrel(opts =>
{
    opts.ListenAnyIP(5020);
});

#endregion

#region Database Configuration

builder.Services.AddSingleton<PostgresDbContext>();

#endregion

#region Services

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISensorRepository, SensorRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton<MQTTtoAMQP>();
builder.Services.AddSingleton<MosquittoConfig>();
builder.Services.AddSingleton<MQTTService>();

builder.Services.AddSingleton<RabbitmqConfig>();
builder.Services.AddSingleton<InfluxAmqpConfig>();
builder.Services.AddSingleton<AMQPService>();
builder.Services.AddSingleton<SensorMessageDispatcher>();
builder.Services.AddSingleton<InfluxDBConfiguration>();
builder.Services.AddSingleton<InfluxService>();
builder.Services.AddSingleton<DeviceHealthService>();

#endregion

#region Jwt Configuration

var jwtSettings = builder.Configuration.GetSection("Jwt");

var key = Encoding.UTF8.GetBytes(
    jwtSettings["key"] ?? throw new InvalidOperationException("JWT key is missing"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId =
        builder.Configuration["GoogleAuth:ClientId"];

    options.ClientSecret =
        builder.Configuration["GoogleAuth:ClientSecret"];
})
.AddJwtBearer(ops =>
{
    // Keep JWT claim type names as issued
    // userId / nameid / sub.

    ops.MapInboundClaims = false;

    ops.TokenValidationParameters =
        new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            NameClaimType = "userId",
            RoleClaimType = "role",

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(key)
        };

    ops.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken =
                context.Request.Query["access_token"];

            var path =
                context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken)
                && path.StartsWithSegments("/sensorHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

#endregion

#region Adding Controllers

builder.Services.AddControllers();

#endregion

#region CORS Configuration

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowDashboard", policy =>
    {
        policy.WithOrigins(
                "http://monitex.local",
                "http://monitex.local:4200",
                "http://localhost:4200",
                "http://localhost:5020")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

#endregion

#region MQTT <-> AMQP Pipeline

builder.Services.Configure<MosquittoSettings>(
    builder.Configuration.GetSection("MQTT")
);

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq")
);

builder.Services.Configure<InfluxPipelineSettings>(
    builder.Configuration.GetSection("InfluxPipeline")
);

#endregion

#region External Services

// Do not start RabbitMQ consumers during integration tests.

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<AMQPtoInfluxConsumer>();
    builder.Services.AddHostedService<AMQPtoSignalRConsumer>();
    builder.Services.AddHostedService<AMQPtoAnomalySignalRConsumer>();
}

#endregion

var app = builder.Build();

#region External Connections

// Do not connect to MQTT or RabbitMQ during integration tests.

if (!app.Environment.IsEnvironment("Testing"))
{
    var mqtt = app.Services.GetRequiredService<MQTTService>();

    await mqtt.Listen();

    var bridge = app.Services.GetRequiredService<MQTTtoAMQP>();

    bridge.start();
}

#endregion

app.UseCors("AllowDashboard");

app.UseHttpsRedirection();

app.UseRequestLogging();

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<SensorHub>("/sensorHub");

app.MapControllers();

app.Run();

public partial class Program { }