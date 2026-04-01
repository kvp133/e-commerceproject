namespace ECommerce.Shared.Events;

public class ProductUpdatedEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}
