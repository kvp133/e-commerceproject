namespace ECommerce.Shared.Kafka;

public static class KafkaTopics
{
    public const string ProductCreated = "product-created";
    public const string ProductUpdated = "product-updated";
    public const string OrderCreated = "order-created";
    public const string OrderStatusChanged = "order-status-changed";
}
