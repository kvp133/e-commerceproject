using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllAsync(cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto, CancellationToken cancellationToken)
    {
        var order = await _orderService.UpdateStatusAsync(id, dto, cancellationToken);
        if (order == null) return NotFound();
        return Ok(order);
    }
}
