namespace API.Services;

using System.Text;
using RabbitMQ.Client;

public class QueueService(IConnectionFactory connectionFactory)
{
    private const int MillisecondsToFiveMinutes = 300000;
    private IConnection? _connection;
    private const string RetryExchangeName = "RetryExchange";

    public async Task SendMessage(string queueName, string messageJson)
    {
        if (_connection is null || _connection.IsOpen == false)
            _connection = await connectionFactory.CreateConnectionAsync();

        await using var model = await _connection.CreateChannelAsync();

        await model.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            new Dictionary<string, object>
                { { "x-consumer-timeout", "undefined" } }!);

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        await model.BasicPublishAsync(
            string.Empty,
            queueName,
            false,
            properties,
            Encoding.UTF8.GetBytes(messageJson));

        await model.CloseAsync();
    }

    public async Task SendDelayedMessageForRetryExchange(
        string queueName,
        string messageJson,
        int time,
        string routingKey)
    {
        if (_connection is null || _connection.IsOpen == false)
            _connection = await connectionFactory.CreateConnectionAsync();

        await using var model = await _connection.CreateChannelAsync();
        
        await model.ExchangeDeclareAsync(RetryExchangeName,
            ExchangeType.Direct, 
            true, 
            false);
        
        await model.QueueDeclareAsync(queueName, false, false, true,
            new Dictionary<string, object>
            {
                { "x-message-ttl", time },
                { "x-dead-letter-exchange", RetryExchangeName },
                { "x-dead-letter-routing-key", routingKey },
                { "x-expires", time + MillisecondsToFiveMinutes }
            }!);
        
        await model.QueueBindAsync(routingKey, RetryExchangeName, routingKey);

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        await model.BasicPublishAsync(
            string.Empty,
            queueName,
            false,
            properties,
            Encoding.UTF8.GetBytes(messageJson));

        await model.CloseAsync();
    }

    public async Task SendMessageWithDelay(string exchangeName, string messageJson,
        string routingKey, int delayMilliseconds)
    {
        if (_connection is null || !_connection.IsOpen)
            _connection = await connectionFactory.CreateConnectionAsync();

        await using var model = await _connection.CreateChannelAsync();

        await model.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: "x-delayed-message",
            durable: true,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-delayed-type", "direct" }
            }!);

        await model.QueueBindAsync(
            queue: routingKey,
            exchange: exchangeName,
            routingKey: routingKey
        );

        var props = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
                { ["x-delay"] = delayMilliseconds },
            DeliveryMode = DeliveryModes.Persistent
        };

        var body = Encoding.UTF8.GetBytes(messageJson);

        await model.BasicPublishAsync(
            exchangeName,
            routingKey,
            false,
            props,
            body);

        await model.CloseAsync();
    }
}