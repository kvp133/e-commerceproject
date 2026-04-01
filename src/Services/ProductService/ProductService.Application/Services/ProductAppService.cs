using ECommerce.Shared.Events;
using ECommerce.Shared.Kafka;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Services;

public class ProductAppService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IKafkaProducer _kafkaProducer;

    public ProductAppService(IProductRepository productRepository, IKafkaProducer kafkaProducer)
    {
        _productRepository = productRepository;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product == null ? null : MapToDto(product);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetByCategoryIdAsync(categoryId, cancellationToken);
        return products.Select(MapToDto);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _productRepository.AddAsync(product, cancellationToken);

        await _kafkaProducer.ProduceAsync(KafkaTopics.ProductCreated, created.Id.ToString(), new ProductCreatedEvent
        {
            ProductId = created.Id,
            Name = created.Name,
            Price = created.Price,
            StockQuantity = created.StockQuantity,
            CreatedAt = created.CreatedAt
        }, cancellationToken);

        return MapToDto(created);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null) return null;

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.ImageUrl = dto.ImageUrl;
        product.CategoryId = dto.CategoryId;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);

        await _kafkaProducer.ProduceAsync(KafkaTopics.ProductUpdated, product.Id.ToString(), new ProductUpdatedEvent
        {
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            UpdatedAt = product.UpdatedAt.Value
        }, cancellationToken);

        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null) return false;

        await _productRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            IsActive = product.IsActive
        };
    }
}
