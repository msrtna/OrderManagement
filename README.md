# OrderManagement

A RESTful e-commerce backend built with **ASP.NET Core Web API (.NET
9)** and **Clean Architecture**. The project provides authentication,
product and category management, shopping cart, checkout, order
processing, and shipment tracking.

The solution is designed as a portfolio project demonstrating backend
development practices, separation of concerns, persistence with Entity
Framework Core, automated testing, and containerized deployment.

## Features

### Authentication and authorization

* User registration and login.
* JWT-based authentication.
* Refresh-token support.
* Role-based authorization.
* Admin-only shipment status management.
* Development Admin account seeding for local and Docker development.

### Catalog management

* Product management.
* Category management.
* Category activation and deactivation.

### Shopping and checkout

* Shopping cart management.
* User address management.
* Checkout and order creation.
* Mock payment processing.

### Orders

* View the authenticated user's orders with pagination.
* Retrieve order details.
* Retrieve order status history.

### Shipments

* Automatic shipment creation after successful payment.
* Initial shipment status: `Preparing`.
* Admin can update shipment status.
* Supported shipment statuses: `Preparing`, `Shipped`, `Delivered`,
and `Cancelled`.
* Shipment status history is recorded.

### Testing and deployment

* Unit tests.
* Integration tests.
* SQLite-based integration testing.
* Docker support for the API and SQL Server.

## Technology Stack

Area                        Technology

\---

Framework                   ASP.NET Core Web API
Runtime                     .NET 9
Architecture                Clean Architecture
Language                    C#
ORM                         Entity Framework Core 9
Database                    Microsoft SQL Server
Authentication              ASP.NET Core Identity
Authorization               JWT Bearer and role-based authorization
API documentation           Swagger / OpenAPI
Validation                  FluentValidation
Testing                     xUnit
Integration test database   SQLite
Containers                  Docker and Docker Compose
Version control             Git

## Solution Structure

The solution is separated into the following projects:

``` text
OrderManagement/
├── OrderManagement.Api/
├── OrderManagement.Application/
├── OrderManagement.Domain/
├── OrderManagement.Infrastructure/
└── OrderManagement.Tests/
```

* **OrderManagement.Api** --- HTTP endpoints, controllers,
authentication configuration, and application startup.
* **OrderManagement.Application** --- application use cases, handlers,
validation, and abstractions.
* **OrderManagement.Domain** --- domain entities, enums, and core
business rules.
* **OrderManagement.Infrastructure** --- Entity Framework Core,
database configurations, Identity, and infrastructure services.
* **OrderManagement.Tests** --- automated unit and integration tests.

## Architecture Overview

The application follows Clean Architecture principles:

* Domain contains the core business model and does not depend on
infrastructure.
* Application contains use cases and defines abstractions required by
those use cases.
* Infrastructure implements persistence and external technical
concerns.
* API is the entry point and composes the application through
dependency injection.

This separation keeps business logic independent of database and HTTP
implementation details and makes the application easier to test and
extend.

## Order and Shipment Workflow

1. An authenticated customer adds products to the shopping cart.
2. The customer submits checkout information and creates an order.
3. The customer initiates payment through the mock payment flow.
4. When payment succeeds, the order is marked as paid, stock is
updated, and the cart is cleared.
5. A shipment is created automatically with the `Preparing` status.
6. An administrator can update the shipment status as it progresses.
7. Shipment status changes are recorded in shipment status history.

Shipment status transitions are restricted by the application:

``` text
Preparing ──> Shipped ──> Delivered
    │             │
    └──> Cancelled <────┘
```

`Delivered` and `Cancelled` are terminal statuses.

> \*\*Payment note:\*\* Payment processing in this project is a mock
> implementation. No real payment provider or financial transaction is
> connected.

## Getting Started

### Prerequisites

For local development:

* .NET 9 SDK
* SQL Server
* A code editor or IDE such as Visual Studio or Visual Studio Code

For containerized execution:

* Docker Desktop
* Docker Compose

### Configuration

The API reads its database and JWT settings from configuration. For
local execution, configure the relevant values in
`OrderManagement.Api/appsettings.json` or through environment variables.

For Docker Compose, create a `.env` file in the solution root. Do not
commit this file or place real credentials in source control.

Example `.env` file:

``` env
SQL\_SA\_PASSWORD=ReplaceWithA\_Strong\_SqlServer\_Password
JWT\_SECRET\_KEY=ReplaceWithA\_Random\_Secret\_At\_Least\_32\_Bytes
DEVELOPMENT\_ADMIN\_EMAIL=admin@example.local
DEVELOPMENT\_ADMIN\_PASSWORD=ReplaceWithA\_Strong\_Admin\_Password
```

Use strong, unique values for all secrets. The development administrator
password must satisfy the password policy configured by ASP.NET Core
Identity.

The Compose configuration maps these values to the API's configuration
using environment variables.

### Run with Docker Compose

From the solution root, run:

``` bash
docker compose up -d --build
```

Check the containers:

``` bash
docker compose ps
```

View API logs:

``` bash
docker compose logs --tail=100 api
```

When running in the configured Development environment, the application
applies Entity Framework Core migrations before seeding Identity roles
and the development administrator.

Open Swagger:

[http://localhost:8080/swagger](http://localhost:8080/swagger)

To stop the containers:

``` bash
docker compose down
```

To stop the containers and remove the SQL Server data volume, use the
following command only when you intentionally want to delete the Docker
database data:

``` bash
docker compose down -v
```

### Run locally

1. Ensure SQL Server is running.
2. Set the connection string and JWT settings in
`OrderManagement.Api/appsettings.json` or environment variables.
3. Set the Development Admin email and password if you are running in
the Development environment.
4. Apply the database migrations using the configured EF Core
migrations setup.
5. Run the API project:

``` bash
dotnet run --project OrderManagement.Api
```

Swagger is enabled in the Development environment.

## Running Tests

Run the test project from the solution root:

``` bash
dotnet test
```

The integration tests use SQLite so they can exercise persistence
behavior without requiring the Docker SQL Server instance.

## API Documentation

Swagger / OpenAPI provides interactive API documentation when the
application is running in the Development environment.

The API includes endpoints for:

* Authentication
* Products
* Categories
* Shopping cart
* User addresses
* Checkout and payments
* Orders and order history
* Shipments and shipment status history

Exact routes and request schemas are documented by the running Swagger
specification.

## Security Notes

* Do not commit `.env` files, passwords, JWT signing keys, or other
secrets.
* Replace all sample configuration values before running the
application.
* The development administrator seeder is intended for development
environments, not production provisioning.
* Configure HTTPS and production-grade secret management before
deploying to a public environment.

## Current Scope and Future Improvements

Current scope includes the backend API, SQL Server persistence,
automated tests, and Docker-based local execution.

Potential future improvements include: - Integration with a real payment
provider. - Production deployment configuration and observability. - A
dedicated frontend application.

## Author

**Mohammad Ali Moradi**

* GitHub: GitHub.com/msrtna
* LinkedIn:
[linkedin.com/in/mohammad-ali-moradi](https://www.linkedin.com/in/mohammad-ali-moradi/)

\---

If you find this project useful or have suggestions, feel free to open
an issue or submit a pull request.

