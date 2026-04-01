using ECommerce.Shared.Kafka;
using Microsoft.EntityFrameworkCore;
using ProductService.Application.Interfaces;
using ProductService.Application.Services;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Data;
using ProductService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Application Services
builder.Services.AddScoped<IProductService, ProductAppService>();
builder.Services.AddScoped<ICategoryService, CategoryAppService>();

// Kafka
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddSingleton<IKafkaProducer>(new KafkaProducer(kafkaBootstrapServers));

var app = builder.Build();

// Auto migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
