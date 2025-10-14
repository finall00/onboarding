using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Worker.Infrastructure.Data;
using Worker.Infrastructure.RabbitMq;
using Worker.Interfaces;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>  
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    return new ConnectionFactory()
    {
        HostName = settings.Host,
        Port = settings.Port,
        UserName = settings.User,
        Password = settings.Pass,
        AutomaticRecoveryEnabled = true,
        ConsumerDispatchConcurrency = Constants.DefaultConsumerDispatchConcurrency
    };
});

builder.Services.AddHostedService<Worker.Worker>();
var host = builder.Build();
host.Run();