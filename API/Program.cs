using API.Infrastructure.Data;
using API.Infrastructure.Kubernetes;
using API.Infrastructure.RabbitMq;
using API.Interfaces;
using API.Services;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
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
builder.Services.AddScoped<IQueuePublisher, QueuePublisher>();
builder.Services.AddScoped<IKubernetesJobService, KubernetesJobService>();


// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// RabbitMQ
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var uri = new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!);
    Console.WriteLine(uri.ToString());
    return new ConnectionFactory
    {
        Uri = uri,
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


