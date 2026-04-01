# ECommerce Microservices

An e-commerce platform built with **Microservice Architecture**, following **Clean Architecture** and **SOLID** principles.

**Tech Stack:** .NET 8 | Entity Framework Core (Code First) | PostgreSQL (Neon) | Apache Kafka | YARP API Gateway | Docker

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Services](#services)
  - [API Gateway](#api-gateway)
  - [Product Service](#product-service)
  - [Order Service](#order-service)
  - [Shared Library](#shared-library)
- [Kafka Event Flow](#kafka-event-flow)
- [API Endpoints](#api-endpoints)
- [Database Schema](#database-schema)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Configuration](#configuration)
  - [Running the Application](#running-the-application)
- [Technologies & Packages](#technologies--packages)
- [Clean Architecture](#clean-architecture)

---

## Architecture Overview

```
                         ┌─────────────────┐
                         │   API Gateway    │
                         │  (YARP - :5100)  │
                         └────────┬────────┘
                                  │
                    ┌─────────────┴─────────────┐
                    │                           │
           ┌────────▼────────┐         ┌────────▼────────┐
           │ Product Service │         │  Order Service   │
           │     (:5101)     │         │     (:5102)      │
           └────────┬────────┘         └────────┬────────┘
                    │                           │
        ┌───────────┴───┐               ┌───────┴───────────┐
        │               │               │                   │
   ┌────▼────┐   ┌──────▼──────┐   ┌────▼────┐   ┌─────────▼─────────┐
   │PostgreSQL│   │    Kafka    │   │PostgreSQL│   │  Kafka Consumer   │
   │productdb │   │  Producer   │   │ orderdb  │   │ (ProductCreated)  │
   └──────────┘   └──────┬──────┘   └──────────┘   └─────────▲─────────┘
                         │                                    │
                         └──────────── Kafka ─────────────────┘
                                   (localhost:9092)
```

---

## Project Structure

```
ECommerce.Microservices/
├── docker-compose.yml
├── ECommerce.Microservices.slnx
│
├── src/
│   ├── ApiGateway/                          # YARP Reverse Proxy (Port 5100)
│   │   ├── Program.cs
│   │   └── appsettings.json                 # Route & Cluster config
│   │
│   ├── Shared/
│   │   └── ECommerce.Shared/                # Shared library
│   │       ├── Events/                      # Kafka event contracts
│   │       │   ├── ProductCreatedEvent.cs
│   │       │   ├── ProductUpdatedEvent.cs
│   │       │   ├── OrderCreatedEvent.cs
│   │       │   └── OrderStatusChangedEvent.cs
│   │       └── Kafka/                       # Kafka infrastructure
│   │           ├── IKafkaProducer.cs
│   │           ├── KafkaProducer.cs
│   │           ├── KafkaConsumerBackgroundService.cs
│   │           └── KafkaTopics.cs
│   │
│   └── Services/
│       ├── ProductService/                  # Product Microservice (Port 5101)
│       │   ├── ProductService.Domain/
│       │   │   ├── Entities/                # Product, Category
│       │   │   └── Interfaces/              # IProductRepository, ICategoryRepository
│       │   ├── ProductService.Application/
│       │   │   ├── DTOs/                    # ProductDto, CreateProductDto, ...
│       │   │   ├── Interfaces/              # IProductService, ICategoryService
│       │   │   └── Services/                # ProductAppService, CategoryAppService
│       │   ├── ProductService.Infrastructure/
│       │   │   ├── Data/                    # ProductDbContext (EF Core)
│       │   │   └── Repositories/            # ProductRepository, CategoryRepository
│       │   └── ProductService.API/
│       │       ├── Controllers/             # ProductsController, CategoriesController
│       │       └── Program.cs               # DI & Middleware
│       │
│       └── OrderService/                    # Order Microservice (Port 5102)
│           ├── OrderService.Domain/
│           │   ├── Entities/                # Order, OrderItem, OrderStatus
│           │   └── Interfaces/              # IOrderRepository
│           ├── OrderService.Application/
│           │   ├── DTOs/                    # OrderDto, CreateOrderDto, ...
│           │   ├── Interfaces/              # IOrderService
│           │   └── Services/                # OrderAppService
│           ├── OrderService.Infrastructure/
│           │   ├── Data/                    # OrderDbContext (EF Core)
│           │   ├── Repositories/            # OrderRepository
│           │   └── Consumers/               # ProductCreatedConsumer (Kafka)
│           └── OrderService.API/
│               ├── Controllers/             # OrdersController
│               └── Program.cs               # DI & Middleware
```

---

## Services

### API Gateway

| Property | Value |
|----------|-------|
| Port | `5100` |
| Technology | YARP (Yet Another Reverse Proxy) v2.1.0 |

**Routing Rules:**

| Route | Destination |
|-------|-------------|
| `api/products/**` | ProductService (`localhost:5101`) |
| `api/categories/**` | ProductService (`localhost:5101`) |
| `api/orders/**` | OrderService (`localhost:5102`) |

---

### Product Service

| Property | Value |
|----------|-------|
| Port | `5101` |
| Database | `productdb` (PostgreSQL - Neon) |
| Kafka Role | **Producer** (product-created, product-updated) |

**Entities:**
- **Product** - Id, Name, Description, Price, StockQuantity, ImageUrl, CategoryId, IsActive, CreatedAt, UpdatedAt
- **Category** - Id, Name, Description, CreatedAt

---

### Order Service

| Property | Value |
|----------|-------|
| Port | `5102` |
| Database | `orderdb` (PostgreSQL - Neon) |
| Kafka Role | **Producer** (order-created, order-status-changed) + **Consumer** (product-created) |

**Entities:**
- **Order** - Id, CustomerId, CustomerEmail, ShippingAddress, Status, TotalAmount, Items, CreatedAt, UpdatedAt
- **OrderItem** - Id, OrderId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice (computed)

**Order Status Flow:**
```
Pending → Confirmed → Processing → Shipped → Delivered
                                             ↘ Cancelled
```

---

### Shared Library

Chứa Kafka infrastructure dùng chung giữa các microservices:

| Component | Description |
|-----------|-------------|
| `IKafkaProducer` | Interface cho Kafka producer |
| `KafkaProducer` | Implementation với Acks.All, Idempotence |
| `KafkaConsumerBackgroundService<T>` | Abstract base class cho consumers (BackgroundService) |
| `KafkaTopics` | Constants cho tên các topic |
| `Events/*` | Event contracts (ProductCreated, ProductUpdated, OrderCreated, OrderStatusChanged) |

---

## Kafka Event Flow

```
┌─────────────────┐     product-created      ┌──────────────────┐
│  ProductService  │ ──────────────────────► │   OrderService    │
│                  │     product-updated      │  (Consumer)       │
│   (Producer)     │ ──────────────────────► │                   │
└─────────────────┘                          └──────────────────┘

┌──────────────────┐     order-created
│   OrderService   │ ──────────────────────► (Available for other consumers)
│                  │     order-status-changed
│   (Producer)     │ ──────────────────────► (Available for other consumers)
└──────────────────┘
```

**Topics:**

| Topic | Producer | Consumer | Trigger |
|-------|----------|----------|---------|
| `product-created` | ProductService | OrderService | Khi tạo mới product |
| `product-updated` | ProductService | - | Khi cập nhật product |
| `order-created` | OrderService | - | Khi tạo mới order |
| `order-status-changed` | OrderService | - | Khi thay đổi status order |

**Kafka Configuration:**
- **Producer:** `Acks = All`, `EnableIdempotence = true` (đảm bảo exactly-once delivery)
- **Consumer:** `AutoOffsetReset = Earliest`, `EnableAutoCommit = false` (manual commit)

---

## API Endpoints

### Products (`/api/products`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/products` | Lấy tất cả products |
| `GET` | `/api/products/{id}` | Lấy product theo ID |
| `GET` | `/api/products/category/{categoryId}` | Lấy products theo category |
| `POST` | `/api/products` | Tạo product mới |
| `PUT` | `/api/products/{id}` | Cập nhật product |
| `DELETE` | `/api/products/{id}` | Xoá product |

### Categories (`/api/categories`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/categories` | Lấy tất cả categories |
| `GET` | `/api/categories/{id}` | Lấy category theo ID |
| `POST` | `/api/categories` | Tạo category mới |
| `DELETE` | `/api/categories/{id}` | Xoá category |

### Orders (`/api/orders`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/orders` | Lấy tất cả orders |
| `GET` | `/api/orders/{id}` | Lấy order theo ID |
| `GET` | `/api/orders/customer/{customerId}` | Lấy orders theo customer |
| `POST` | `/api/orders` | Tạo order mới |
| `PATCH` | `/api/orders/{id}/status` | Cập nhật trạng thái order |

### Request/Response Examples

**Create Product:**
```json
POST /api/products
{
  "name": "iPhone 15 Pro",
  "description": "Apple iPhone 15 Pro 256GB",
  "price": 999.99,
  "stockQuantity": 100,
  "imageUrl": "https://example.com/iphone15.jpg",
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Create Order:**
```json
POST /api/orders
{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerEmail": "customer@example.com",
  "shippingAddress": "123 Main St, City",
  "items": [
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "productName": "iPhone 15 Pro",
      "quantity": 2,
      "unitPrice": 999.99
    }
  ]
}
```

**Update Order Status:**
```json
PATCH /api/orders/{id}/status
{
  "status": "Confirmed"
}
```

---

## Database Schema

### ProductDB

```
┌─────────────────────────┐       ┌──────────────────────────┐
│       Categories        │       │        Products          │
├─────────────────────────┤       ├──────────────────────────┤
│ Id          (PK, Guid)  │◄──────│ Id            (PK, Guid) │
│ Name        (Required)  │       │ Name          (Required)  │
│ Description             │       │ Description               │
│ CreatedAt               │       │ Price         (Decimal)   │
└─────────────────────────┘       │ StockQuantity (Int)       │
    Unique Index: Name            │ ImageUrl                  │
                                  │ CategoryId    (FK)        │
                                  │ IsActive      (Bool)      │
                                  │ CreatedAt                 │
                                  │ UpdatedAt                 │
                                  └──────────────────────────┘
                                      Index: CategoryId, Name
```

### OrderDB

```
┌──────────────────────────┐       ┌──────────────────────────┐
│         Orders           │       │       OrderItems          │
├──────────────────────────┤       ├──────────────────────────┤
│ Id              (PK)     │◄──────│ Id            (PK, Guid) │
│ CustomerId      (Guid)   │       │ OrderId       (FK)       │
│ CustomerEmail   (Req.)   │       │ ProductId     (Guid)     │
│ ShippingAddress (Req.)   │       │ ProductName   (Required) │
│ Status          (String) │       │ Quantity      (Int)      │
│ TotalAmount     (Decimal)│       │ UnitPrice     (Decimal)  │
│ CreatedAt                │       └──────────────────────────┘
│ UpdatedAt                │           Cascade Delete
└──────────────────────────┘
    Index: CustomerId, Status
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [PostgreSQL (Neon)](https://neon.tech/) account (hoặc PostgreSQL local)

### Configuration

**1. Clone repository:**
```bash
git clone https://github.com/kvp133/e-commerceproject.git
cd e-commerceproject
```

**2. Cấu hình PostgreSQL connection string:**

Tạo 2 database trên Neon (hoặc PostgreSQL local): `productdb` và `orderdb`.

Cập nhật connection string trong:

- `src/Services/ProductService/ProductService.API/appsettings.json`
- `src/Services/OrderService/OrderService.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<your-neon-host>.neon.tech;Database=productdb;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

**3. Khởi động Kafka:**
```bash
docker compose up -d
```

Kafka UI sẽ chạy tại `http://localhost:8080`.

### Running the Application

**Chạy từng service (mỗi terminal riêng):**

```bash
# Terminal 1 - Product Service
dotnet run --project src/Services/ProductService/ProductService.API

# Terminal 2 - Order Service
dotnet run --project src/Services/OrderService/OrderService.API

# Terminal 3 - API Gateway
dotnet run --project src/ApiGateway
```

**Truy cập:**

| Service | URL |
|---------|-----|
| API Gateway | http://localhost:5100 |
| Product Service (Swagger) | http://localhost:5101/swagger |
| Order Service (Swagger) | http://localhost:5102/swagger |
| Kafka UI | http://localhost:8080 |

> **Note:** EF Core sẽ tự động chạy migration khi khởi động service. Database schema sẽ được tạo tự động (Code First).

---

## Technologies & Packages

| Category | Technology | Version |
|----------|-----------|---------|
| **Framework** | .NET | 8.0 |
| **ORM** | Entity Framework Core | 8.0.11 |
| **Database** | PostgreSQL (Neon) | - |
| **DB Provider** | Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| **Message Broker** | Apache Kafka | 3.7.0 |
| **Kafka Client** | Confluent.Kafka | 2.3.0 |
| **API Gateway** | YARP (Yet Another Reverse Proxy) | 2.1.0 |
| **API Docs** | Swashbuckle (Swagger) | 6.6.2 |
| **Serialization** | System.Text.Json | 8.0.5 |
| **Containerization** | Docker Compose | - |

---

## Clean Architecture

Mỗi microservice tuân thủ **Clean Architecture** với 4 layers:

```
┌──────────────────────────────────────┐
│              API Layer               │  ← Controllers, Program.cs, DI
├──────────────────────────────────────┤
│        Infrastructure Layer          │  ← EF Core, Repositories, Kafka Consumers
├──────────────────────────────────────┤
│         Application Layer            │  ← DTOs, Services, Interfaces
├──────────────────────────────────────┤
│           Domain Layer               │  ← Entities, Repository Interfaces
└──────────────────────────────────────┘
```

**Dependency Rule:** Dependencies chỉ đi từ ngoài vào trong.

```
API → Infrastructure → Application → Domain
```

- **Domain** - Không phụ thuộc vào bất kỳ layer nào. Chứa Entities và Repository Interfaces.
- **Application** - Phụ thuộc Domain. Chứa business logic, DTOs, Service Interfaces.
- **Infrastructure** - Phụ thuộc Application. Implement Repository Interfaces, DbContext, Kafka.
- **API** - Phụ thuộc Infrastructure & Application. Controllers, DI configuration.

**SOLID Principles Applied:**
- **S** - Single Responsibility: Mỗi class chỉ có một nhiệm vụ (Repository, Service, Controller)
- **O** - Open/Closed: Mở rộng qua interfaces, không sửa đổi code cũ
- **L** - Liskov Substitution: Implementations có thể thay thế interfaces
- **I** - Interface Segregation: Interfaces nhỏ, chuyên biệt (IProductRepository, ICategoryRepository)
- **D** - Dependency Inversion: Layers trên phụ thuộc vào abstractions, không phụ thuộc implementations
