using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Worker.Infrastructure.Data;
using Worker.Infrastructure.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker.Worker>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));



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

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    
    return new ConnectionFactory()
    {
        HostName = settings.Host,
        Port = settings.Port,
        UserName = settings.Username,
        Password = settings.Password,
        AutomaticRecoveryEnabled = true,
        ConsumerDispatchConcurrency = Constants.DefaultConsumerDispatchConcurrency
    };
});

builder.Services.AddSingleton<IRabbitMqConsumer, RabbitMqConsumer>();

builder.Services.AddHostedService<Worker.Worker>();

var host = builder.Build();

host.Run();
