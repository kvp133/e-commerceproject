using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Shared.Kafka;

public abstract class KafkaConsumerBackgroundService<T> : BackgroundService
{
    private readonly string _topic;
    private readonly string _groupId;
    private readonly string _bootstrapServers;
    private readonly ILogger _logger;

    protected KafkaConsumerBackgroundService(string bootstrapServers, string topic, string groupId, ILogger logger)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
        _groupId = groupId;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);

        _logger.LogInformation("Kafka consumer started for topic {Topic} with group {GroupId}", _topic, _groupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value == null) continue;

                var message = JsonSerializer.Deserialize<T>(result.Message.Value);
                if (message != null)
                {
                    await HandleAsync(message, stoppingToken);
                }

                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message from topic {Topic}", _topic);
            }
        }

        consumer.Close();
    }

    protected abstract Task HandleAsync(T message, CancellationToken cancellationToken);
}
