# Опис C4 діаграм

## Level 1 — System Context

Діаграма показує Nimble.Modulith як єдину систему, з якою взаємодіють користувачі та адміністратор. Також показано зовнішні елементи: SQL Server, Papercut SMTP та .NET Aspire Dashboard.

## Level 2 — Container

Діаграма деталізує контейнери системи. Основним контейнером є Web API, який підключає модулі Users, Products, Customers, Email та Reporting. Також показані окремі бази даних для модулів і Papercut SMTP для тестування email.

## Level 3 — Component

Діаграма показує внутрішню компонентну структуру Web API та основних модулів. Вона демонструє використання FastEndpoints, Mediator, EF Core, ASP.NET Core Identity, background worker та контрактних проєктів.