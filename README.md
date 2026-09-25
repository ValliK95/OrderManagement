# Order Management API

A RESTful Web API to manage customers, products and orders, built with .NET 8, Entity Framework Core and SQL Server.

## Tech Stack

| Area | Technology |
|---|---|
| Framework | .NET 8, ASP.NET Core Web API (controllers) |
| ORM | Entity Framework Core 8 (Code-First, Migrations) |
| Database | SQL Server (LocalDB by default) |
| Validation | FluentValidation |
| API documentation | Swagger / OpenAPI (Swashbuckle) |
| Error handling | .NET 8 `IExceptionHandler` with ProblemDetails |

## Project Structure

```
src/
  OrderManagement.Api             Controllers, Program.cs, exception handler, Swagger
  OrderManagement.Application     Services, DTOs, validators, IAppDbContext
  OrderManagement.Domain          Entities and business rules (no dependencies)
  OrderManagement.Infrastructure  AppDbContext, EF Core configurations, migrations
postman/                          Postman collection
```

**Dependency direction:** Api → Application → Domain, and Infrastructure → Application. The Domain project depends on nothing. The Api project also references Infrastructure, but only to register its services at startup. Each layer registers its own services through an extension method (`AddApplication`, `AddInfrastructure`), keeping `Program.cs` short.

**Request flow:** Controller → Service (validation and business rules) → AppDbContext (EF Core) → SQL Server. Any exception thrown along the way is converted into the right HTTP response by `GlobalExceptionHandler`.

## Setup Instructions

**Prerequisites**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (check with `dotnet --version`)
- SQL Server: LocalDB (installed with Visual Studio), SQL Server Express, or SQL Server Developer edition
- EF Core CLI tool (version 8, to match EF Core 8):
  ```bash
  dotnet tool install --global dotnet-ef --version 8.*
  ```
  If it is already installed, check the version with `dotnet ef --version`.

**Steps**
```bash
git clone https://github.com/ValliK95/OrderManagement.git
cd OrderManagement
dotnet build
```

If the browser shows a certificate warning when running over HTTPS, trust the local development certificate once:

```bash
dotnet dev-certs https --trust
```

## Database Configuration

The connection string is in `src/OrderManagement.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=OrderManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

By default it uses **SQL Server LocalDB** with Windows Authentication. To use a different SQL Server instance, change the `Server` value (and add a user id and password if Windows Authentication is not used).

Create the database by applying the migrations:

```bash
dotnet ef database update --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.Api
```

The database `OrderManagementDb` does not need to be created manually: `dotnet ef database update` creates it along with all tables.

Migration files are in `src/OrderManagement.Infrastructure/Migrations`. The schema is created by a single `InitialCreate` migration with these tables:

- **Customers**: Id, FullName, Email (unique), CreatedDate
- **Products**: Id, Name, Sku (unique), Price, StockQuantity
- **Orders**: Id, CustomerId, OrderDate, Status
- **OrderItems**: Id, OrderId, ProductId, Quantity, UnitPrice

To add a new migration after changing the model:

```bash
dotnet ef migrations add <MigrationName> --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.Api --output-dir Migrations
dotnet ef database update --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.Api
```

## How to Run the Application

```bash
dotnet run --project src/OrderManagement.Api
```

Open `https://localhost:<port>/swagger` in a browser (the port is shown in the console).

A Postman collection is included in the `postman` folder. Import it, set the `baseUrl` variable to the API address, and run the requests in order (customers and products first, then orders), since orders need an existing customer and product.

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/customers` | Create a customer |
| GET | `/api/customers/{id}` | Get a customer by id |
| GET | `/api/customers` | Get all customers |
| POST | `/api/products` | Create a product |
| GET | `/api/products/{id}` | Get a product by id |
| GET | `/api/products` | Get all products |
| GET | `/api/products?name={text}` | Search products by name |
| PATCH | `/api/products/{id}/stock` | Update stock quantity |
| POST | `/api/orders` | Create an order |
| GET | `/api/orders/{id}` | Get order details with customer and items |
| PATCH | `/api/orders/{id}/status` | Update order status |

---

## Design Decisions and Assumptions

### Architecture

- A lightweight Clean Architecture with four projects keeps business logic separate from the API and the database, making it easy to read, test and extend.
- Services use an `IAppDbContext` interface instead of a repository layer, because EF Core's DbContext already works as a repository and unit of work. CQRS and MediatR were not used, to avoid unnecessary complexity for this scope.
- Request and response DTOs are separate from entities, so the API never exposes database models directly. Requests only contain what the client is allowed to set.
- Controllers are thin: they receive the request, call a service and return the result, with no try-catch blocks. All error handling happens in one global exception handler.
- Entity rules (lengths, unique indexes, decimal precision, relationships) are defined with Fluent API configuration classes, keeping the Domain entities free of database attributes.

### Customers

- Email must be unique. It is trimmed and stored in lowercase, so `Valli@Test.com` and `valli@test.com` are treated as the same customer.
- `CreatedDate` is set by the server in UTC and cannot be sent by the client.

### Products

- SKU must be unique. It is trimmed and stored in uppercase.
- "Retrieve all products" and "Search products by name" share one endpoint: `GET /api/products` returns all products, and `GET /api/products?name=xyz` returns products whose name contains the text (case-insensitive). This follows the REST convention of filtering a collection with query parameters and avoids duplicating the query logic.
- `GET /api/products/{id}` was added (not listed in the requirements) so the create endpoint can return `201 Created` with a `Location` header pointing to the new product.
- Updating stock **sets** the stock to the new value; it does not add to or subtract from the current value.
- Price must be greater than 0 and stock cannot be negative.

### Orders

- An order must contain at least one item, and each quantity must be greater than 0.
- The client only sends the customer id, product ids and quantities. Order date, status and unit price are set by the server, so they cannot be manipulated.
- **Unit price is copied from the product at the time of ordering.** Past orders keep the price the customer paid, even if the product price changes later.
- If the same product appears more than once in a request, the quantities are combined into one line before the stock check.
- Stock is checked for all items before anything is saved. If any item has insufficient stock, the whole order is rejected with `409 Conflict`, and all shortages are listed in the message.
- The order, its items and the stock reduction are saved in a **single `SaveChangesAsync` call**, so they succeed or fail together.
- New orders always start with the status `Pending`.
- The order details response includes the order total and each item's product name. These are calculated at query time and not stored.

### Order Status

- Allowed status changes:
  - `Pending` → `Confirmed` → `Shipped` → `Delivered`
  - `Pending` or `Confirmed` → `Cancelled`
- Any other change (for example `Delivered` → `Pending`) returns `409 Conflict`. The rules live in the `Order` entity (`CanChangeStatusTo`), so they are defined in one place.
- **Cancelling an order returns its items to stock.** The status rules also make sure stock can never be returned twice.
- Status values are sent and returned as text (for example `"Confirmed"`), not numbers.

### Data and Relationships

- Deleting a customer or product that has orders is blocked at the database level (`Restrict`), protecting order history. Deleting an order deletes its items (`Cascade`). No delete endpoints were required.
- Prices are stored as `decimal(18,2)`.
- All dates are stored in UTC.

### Error Handling

- Services throw meaningful exceptions (`NotFoundException`, `ConflictException`, FluentValidation's `ValidationException`), and `GlobalExceptionHandler` maps them to 404, 409 and 400.
- All error responses use the standard ProblemDetails format.
- Unexpected errors return `500` with a generic message; the real details are only written to the log.

## Libraries Used

| Library | Project | Purpose |
|---|---|---|
| Microsoft.EntityFrameworkCore | Application | DbSet and query extensions used by the services |
| Microsoft.EntityFrameworkCore.SqlServer | Infrastructure | SQL Server database provider |
| Microsoft.EntityFrameworkCore.Design | Api | Design-time support for EF Core migrations |
| FluentValidation.DependencyInjectionExtensions | Application | Request validation and automatic validator registration |
| Swashbuckle.AspNetCore | Api | Swagger / OpenAPI documentation and UI |

All libraries are open source and available on NuGet.

---

## Future Improvements

- **Concurrency control on stock:** in a production system, I would add a `RowVersion` column to Product for optimistic concurrency, so two simultaneous orders cannot oversell the same stock. It was left out to keep this task focused on the stated requirements.
- **Pagination** for the "get all" endpoints.
- **Unit and integration tests**, especially for order creation and status rules.
- **Authentication** (for example JWT) and role-based access.
- **Health checks** and structured logging.
