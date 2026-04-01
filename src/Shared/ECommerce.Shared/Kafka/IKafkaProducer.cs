namespace ECommerce.Shared.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
}
