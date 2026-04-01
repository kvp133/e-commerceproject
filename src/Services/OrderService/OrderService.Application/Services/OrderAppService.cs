using ECommerce.Shared.Events;
using ECommerce.Shared.Kafka;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class OrderAppService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IKafkaProducer _kafkaProducer;

    public OrderAppService(IOrderRepository orderRepository, IKafkaProducer kafkaProducer)
    {
        _orderRepository = orderRepository;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        return order == null ? null : MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        return orders.Select(MapToDto);
    }

    public async Task<IEnumerable<OrderDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return orders.Select(MapToDto);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = dto.CustomerId,
            CustomerEmail = dto.CustomerEmail,
            ShippingAddress = dto.ShippingAddress,
            Status = OrderStatus.Pending,
            Items = dto.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        var created = await _orderRepository.AddAsync(order, cancellationToken);

        await _kafkaProducer.ProduceAsync(KafkaTopics.OrderCreated, created.Id.ToString(), new OrderCreatedEvent
        {
            OrderId = created.Id,
            CustomerId = created.CustomerId,
            TotalAmount = created.TotalAmount,
            Items = created.Items.Select(i => new OrderItemEvent
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            CreatedAt = created.CreatedAt
        }, cancellationToken);

        return MapToDto(created);
    }

    public async Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null) return null;

        if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
            return null;

        var oldStatus = order.Status.ToString();
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepository.UpdateAsync(order, cancellationToken);

        await _kafkaProducer.ProduceAsync(KafkaTopics.OrderStatusChanged, order.Id.ToString(), new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus.ToString(),
            ChangedAt = order.UpdatedAt.Value
        }, cancellationToken);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerEmail = order.CustomerEmail,
            ShippingAddress = order.ShippingAddress,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            CreatedAt = order.CreatedAt
        };
    }
}
