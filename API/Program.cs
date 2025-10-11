using API.Infrastructure.Data;
using API.Infrastructure.Kubernetes;
using API.Infrastructure.RabbitMq;
using API.Interfaces;
using API.Services;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
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
builder.Services.AddScoped<IKubernetesJobService, KubernetesJobService>();


// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

//RabbitMQ load settings
builder.Services.AddOptions<RabbitMqSettings>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_HOST")))
            settings.Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST")!;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_PORT")))
            settings.Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT")!);

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_USER")))
            settings.Username = Environment.GetEnvironmentVariable("RABBITMQ_USER")!;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_PASS")))
            settings.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS")!;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE")))
            settings.Exchange = Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE")!;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_QUEUE")))
            settings.QueueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE")!;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_ROUTING_KEY")))
            settings.RoutingKey = Environment.GetEnvironmentVariable("RABBITMQ_ROUTING_KEY")!;

    });

// RabbitMQ
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    return new ConnectionFactory
    {
        HostName = settings.Host,
        Port = settings.Port,
        UserName = settings.Username,
        Password = settings.Password,
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


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Health Check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");
    

app.Run();


