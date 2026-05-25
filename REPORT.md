# Звіт до лабораторного практикуму №2.4: Модульний моноліт на основі DDD / Modulith

## Тема

Практична реалізація модульного моноліту на платформі .NET 10 із використанням ASP.NET Core, FastEndpoints, .NET Aspire, Identity, Mediator, EF Core, SMTP Email module та Reporting module зі star schema.

## Мета роботи

Метою роботи є практичне освоєння архітектури modular monolith, де система запускається як єдиний застосунок, але логічно поділена на ізольовані модулі з чіткими межами відповідальності. Додатково досліджено міжмодульну взаємодію через контракти, рольову авторизацію, доменні події, фонову обробку email-команд і побудову аналітичного reporting-модуля.

## Стек технологій

- C# / .NET 10
- ASP.NET Core Minimal hosting model
- FastEndpoints
- ASP.NET Core Identity
- Entity Framework Core SQL Server
- .NET Aspire AppHost + ServiceDefaults
- Mediator
- Serilog
- MailKit + Papercut SMTP
- Dapper / EF Core для reporting-сценаріїв

## Загальна архітектура

Система складається з таких модулів:

1. `Users` — реєстрація користувачів, Identity, ролі, reset password.
2. `Products` — CRUD для продуктів, admin-only зміни, публічне читання.
3. `Customers` — створення клієнтів, замовлення, підтвердження замовлень.
4. `Email` — асинхронна відправка листів через чергу і background worker.
5. `Reporting` — аналітична БД зі star schema та CSV/JSON звітами.

Кожен модуль має окремий `*.Contracts` проєкт. Це дозволяє іншим модулям використовувати тільки публічні команди, query та events без прямого доступу до внутрішньої реалізації.

## Реалізація лабораторій

### Lab 1

Створено початкову структуру рішення:

- `Nimble.Modulith.AppHost`
- `Nimble.Modulith.ServiceDefaults`
- `Nimble.Modulith.Web`
- `Nimble.Modulith.Users`
- `Nimble.Modulith.Users.Contracts`

Додано `Directory.Build.props` з `TargetFramework=net10.0` та `Directory.Packages.props` для central package management.

### Lab 2

У Web Host налаштовано:

- Serilog logging;
- FastEndpoints;
- JWT Bearer authentication;
- Swagger;
- ASP.NET Core Identity;
- SQL Server database `usersdb` через Aspire;
- seed admin-користувача `admin@nimble.local / admin123`.

Реалізовані endpoint-и:

- `POST /register`
- `POST /login`
- `POST /users/reset-password`
- `POST /users/{id}/roles`

### Lab 3

Додано Products module:

- `Nimble.Modulith.Products`
- `Nimble.Modulith.Products.Contracts`
- `ProductsDbContext`
- entity `Product`
- CRUD API
- seed products
- cross-module queries: `GetProductPriceQuery`, `GetProductDetailsQuery`

Endpoint-и:

- `GET /products`
- `GET /products/{id}`
- `POST /products`
- `PUT /products/{id}`
- `DELETE /products/{id}`

Create/update/delete доступні лише ролі `Admin`.

### Lab 4

Додано Customers module з доменною моделлю:

- `Customer`
- `Address`
- `Order`
- `OrderItem`
- `OrderStatus`

Реалізовано customer/order workflow:

- створення customer;
- створення order;
- додавання item;
- видалення item;
- перегляд order;
- список orders by date.

Реалізовано ownership authorization: admin має повний доступ, звичайний користувач може працювати лише зі своїм customer/order.

### Lab 5

Додано Email module:

- `SendEmailCommand`
- `IQueueService<T>`
- `ChannelQueueService<T>`
- `IEmailSender`
- `SmtpEmailSender`
- `EmailSendingBackgroundWorker`
- Papercut SMTP container в AppHost

Також додано role-based authorization для product management і custom ownership authorization для customer/order management.

### Lab 6

Виправлено проблему pricing manipulation: клієнт більше не передає ціну товару в order request. Customers module отримує product name/price через Products.Contracts:

- `GetProductDetailsQuery`
- `ProductDetailsResult`

Також реалізовано:

- automatic password generation;
- automatic user creation when customer is created;
- welcome email with temporary password;
- reset password endpoint;
- order confirmation email.

### Lab 7

Додано Reporting module зі star schema:

Dimension tables:

- `DimDate`
- `DimCustomer`
- `DimProduct`

Fact table:

- `FactOrders`

Під час `POST /orders/{id}/confirm` публікується `OrderCreatedEvent`, який обробляється Reporting module. Handler створює dimension records за потреби та додає fact rows. Для idempotency використано unique index на `(OrderId, OrderItemId)`.

Reporting endpoints:

- `GET /reports/orders?startDate=2025-01-01&endDate=2025-12-31`
- `GET /reports/orders?...&format=csv`
- `GET /reports/product-sales?...`
- `GET /reports/customers/{customerId}/orders?format=csv`

## Висновок

У результаті виконання практикуму створено повноцінний модульний моноліт, у якому модулі мають власні межі відповідальності, власні контракти та окремі БД. Система демонструє dependency inversion, bounded contexts, role-based security, ownership authorization, event-driven ingestion, transactional emails та reporting architecture зі star schema. Такий підхід дозволяє надалі винести окремий модуль у мікросервіс без переписування доменної логіки.
