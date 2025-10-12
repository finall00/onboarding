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


// TODO: Add RabbitMQ
// TODO: Add Kubernetes
// TODO: valid All inputs with  validator
// TODO: Create a worker for consume the queue

// Postgres
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));

builder.Services.AddScoped<ILeadListService, LeadListService>();
builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

//Select Job Runner
var jobRunner = builder.Configuration.GetValue<string>("JobRunner");
switch (jobRunner)
{
    case "Kubernetes":
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
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

var config = app.Services.GetRequiredService<IConfiguration>();

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
app.MapGet("/ready", () => Results.Ok(new {status = "Ready", timestamp = DateTime.UtcNow })).WithName("Ready");
app.MapGet("/live", () => Results.Ok(new {status ="Alive", timeStamp = DateTime.UtcNow })).WithName("Live");
app.Run();