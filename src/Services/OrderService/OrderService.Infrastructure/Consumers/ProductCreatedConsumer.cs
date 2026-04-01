using ECommerce.Shared.Events;
using ECommerce.Shared.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Consumers;

public class ProductCreatedConsumer : KafkaConsumerBackgroundService<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(ILogger<ProductCreatedConsumer> logger, IConfiguration configuration)
        : base(
            configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            KafkaTopics.ProductCreated,
            "order-service-product-created",
            logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(ProductCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderService received ProductCreated event: {ProductId} - {ProductName} - ${Price}",
            message.ProductId, message.Name, message.Price);
        return Task.CompletedTask;
    }
}
