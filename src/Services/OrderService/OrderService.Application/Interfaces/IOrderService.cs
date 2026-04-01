namespace OrderService.Application.Interfaces;

using OrderService.Application.DTOs;

public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);
    Task<OrderDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto, CancellationToken cancellationToken = default);
}
