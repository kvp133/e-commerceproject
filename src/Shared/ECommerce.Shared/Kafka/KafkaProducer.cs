using System.Text.Json;
using Confluent.Kafka;

namespace ECommerce.Shared.Kafka;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(string bootstrapServers)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = json
        }, cancellationToken);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
