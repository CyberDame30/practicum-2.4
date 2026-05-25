# Nimble.Modulith — Modular Monolith Workshop Labs 1–7

Готова реалізація практикуму NimblePros Modular Monolith Workshop у форматі модульного моноліту на .NET 10.

## Що реалізовано

- Lab 1: структура рішення: AppHost, ServiceDefaults, Web, Users, Users.Contracts.
- Lab 2: FastEndpoints, ASP.NET Core Identity, Serilog, JWT/cookie-ready auth, Swagger.
- Lab 3: Products module з CRUD API та окремою БД `productsdb`.
- Lab 4: Customers/Orders module з Clean Architecture-поділом, CQRS/Mediator-підходом, окремою БД `customersdb`.
- Lab 5: Email module з чергою через `Channel<T>`, background worker, SMTP/Papercut, RBAC для Products, ownership checks для Customers/Orders.
- Lab 6: cross-module communication: Customers бере product name/price через Products.Contracts; генерація паролів; reset password; transactional emails.
- Lab 7: Reporting module зі star schema: DimDate, DimCustomer, DimProduct, FactOrders, ingestion через OrderCreatedEvent, JSON/CSV reports.

## Запуск

```bash
cd Nimble.Modulith
./create-solution.sh
# якщо Aspire CLI не встановлено:
dotnet workload install aspire
# запуск через Aspire AppHost
dotnet run --project Nimble.Modulith.AppHost
```

Після запуску відкрийте Swagger для Web API або використовуйте `Nimble.Modulith.Web/Nimble.Modulith.Web.http`.

## Seed admin

- Email: `admin@nimble.local`
- Password: `admin123`

## Основні endpoint-и

- `POST /register`
- `POST /login`
- `POST /users/reset-password`
- `POST /users/{id}/roles`
- `GET /products`, `POST /products`, `PUT /products/{id}`, `DELETE /products/{id}`
- `POST /customers`, `GET /customers`, `GET /customers/{id}`
- `POST /orders`, `POST /orders/{id}/items`, `DELETE /orders/{id}/items/{itemId}`, `POST /orders/{id}/confirm`
- `GET /reports/orders?startDate=2025-01-01&endDate=2025-12-31&format=json|csv`
- `GET /reports/product-sales?...`
- `GET /reports/customers/{customerId}/orders?format=json|csv`

## Архітектурна ідея

Рішення запускається як один процес, але код розділений на модулі. Кожен модуль має власні контракти, власну БД/схему та власну відповідальність. Міжмодульна взаємодія відбувається через `*.Contracts` і `Mediator`, а не через прямий доступ до внутрішніх класів іншого модуля.
