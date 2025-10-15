using leadListAPI.Infrastructure.Data;
using leadListAPI.Infrastructure.RabbitMq;
using leadListAPI.Interfaces;
using leadListAPI.Services;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using leadListAPI.Infrastructure.WorkerJobCreator.Kubernetes;
using leadListAPI.Infrastructure.WorkerJobCreator.LocalProcess;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Postgres
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));

builder.Services.AddScoped<ILeadListService, LeadListService>();
builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

//Select Job Runner
var jobRunner = builder.Configuration.GetValue<string>("JobRunner");
switch (jobRunner?.ToLower())
{
    case "kubernetes":
        builder.Services.AddScoped<IJobCreator, KubernetesJobCreator>();
        break;
    default:
        builder.Services.AddScoped<IJobCreator, LocalProcessJobCreator>();
        break;
}

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

//RabbitMQ load settings
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

// RabbitMQ
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    Console.WriteLine(settings.Host);
    return new ConnectionFactory
    {
        HostName = settings.Host,
        Port = settings.Port,
        UserName = settings.User,
        Password = settings.Pass,
        ConsumerDispatchConcurrency = Constants.DefaultConsumerDispatchConcurrency
    };
});

// CORS config
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

Console.WriteLine(allowedOrigins);

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
    {
            policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

var app = builder.Build();

// TODO: remove true
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow })).WithName("HealthCheck");
app.Run();