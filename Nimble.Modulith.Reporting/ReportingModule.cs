using System.Text;
using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nimble.Modulith.Customers.Contracts;

namespace Nimble.Modulith.Reporting;

public class DimDate
{
    public int DateKey { get; set; }
    public DateTime Date { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string MonthName { get; set; } = string.Empty;
}

public class DimCustomer
{
    public int CustomerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class DimProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public class FactOrder
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int OrderItemId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int DateKey { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal OrderTotalAmount { get; set; }
}

public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<DimDate> DimDates => Set<DimDate>();
    public DbSet<DimCustomer> DimCustomers => Set<DimCustomer>();
    public DbSet<DimProduct> DimProducts => Set<DimProduct>();
    public DbSet<FactOrder> FactOrders => Set<FactOrder>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("Reporting");

        builder.Entity<DimDate>(entity =>
        {
            entity.ToTable("DimDate");
            entity.HasKey(x => x.DateKey);
            entity.Property(x => x.DateKey).ValueGeneratedNever();

            var dates = Enumerable.Range(0, 365)
                .Select(i => new DateTime(2025, 1, 1).AddDays(i))
                .Select(d => new DimDate
                {
                    DateKey = d.Year * 10000 + d.Month * 100 + d.Day,
                    Date = d,
                    Year = d.Year,
                    Quarter = (d.Month - 1) / 3 + 1,
                    Month = d.Month,
                    Day = d.Day,
                    DayOfWeek = (int)d.DayOfWeek,
                    DayName = d.DayOfWeek.ToString(),
                    MonthName = d.ToString("MMMM")
                });

            entity.HasData(dates);
        });

        builder.Entity<DimCustomer>(entity =>
        {
            entity.ToTable("DimCustomer");
            entity.HasKey(x => x.CustomerId);
            entity.Property(x => x.CustomerId).ValueGeneratedNever();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.FullName).HasMaxLength(200);
        });

        builder.Entity<DimProduct>(entity =>
        {
            entity.ToTable("DimProduct");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.ProductId).ValueGeneratedNever();
            entity.Property(x => x.ProductName).HasMaxLength(200);
        });

        builder.Entity<FactOrder>(entity =>
        {
            entity.ToTable("FactOrders");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.OrderId, x.OrderItemId }).IsUnique();
            entity.Property(x => x.OrderNumber).HasMaxLength(50);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.OrderTotalAmount).HasColumnType("decimal(18,2)");
        });
    }
}

public static class ReportingModuleExtensions
{
    public static IHostApplicationBuilder AddReportingModuleServices(
        this IHostApplicationBuilder builder,
        Serilog.ILogger logger)
    {
        builder.AddSqlServerDbContext<ReportingDbContext>("reportingdb");
        logger.Information("Reporting module services registered");
        return builder;
    }

    public static async Task<WebApplication> EnsureReportingModuleDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        if (env.IsDevelopment())
        {
            await db.Database.EnsureDeletedAsync();
        }

        await db.Database.EnsureCreatedAsync();
        return app;
    }
}

public class OrderCreatedEventHandler(ReportingDbContext db, ILogger<OrderCreatedEventHandler> logger)
    : INotificationHandler<OrderCreatedEvent>
{
    public async ValueTask Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        var dateKey = notification.OrderDate.Year * 10000
            + notification.OrderDate.Month * 100
            + notification.OrderDate.Day;

        if (!await db.DimCustomers.AnyAsync(x => x.CustomerId == notification.CustomerId, cancellationToken))
        {
            db.DimCustomers.Add(new DimCustomer
            {
                CustomerId = notification.CustomerId,
                Email = notification.CustomerEmail,
                FullName = notification.CustomerName
            });
        }

        foreach (var item in notification.Items)
        {
            if (!await db.DimProducts.AnyAsync(x => x.ProductId == item.ProductId, cancellationToken))
            {
                db.DimProducts.Add(new DimProduct
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName
                });
            }

            var alreadyExists = await db.FactOrders.AnyAsync(
                x => x.OrderId == notification.OrderId && x.OrderItemId == item.OrderItemId,
                cancellationToken);

            if (!alreadyExists)
            {
                db.FactOrders.Add(new FactOrder
                {
                    OrderId = notification.OrderId,
                    OrderItemId = item.OrderItemId,
                    OrderNumber = notification.OrderNumber,
                    DateKey = dateKey,
                    CustomerId = notification.CustomerId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    OrderTotalAmount = notification.OrderTotal
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order {OrderNumber} ingested into reporting", notification.OrderNumber);
    }
}

public record OrdersReportRow(string OrderNumber, DateTime Date, string CustomerName, decimal Total);
public record ProductSalesRow(int ProductId, string ProductName, int QuantitySold, decimal Revenue, int OrderCount);
public record CustomerOrdersRow(string OrderNumber, DateTime Date, decimal Total);

public static class CsvFormatter
{
    public static string Format<T>(IEnumerable<T> rows)
    {
        var properties = typeof(T).GetProperties();
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(',', properties.Select(p => Escape(p.Name))));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', properties.Select(p => Escape(Convert.ToString(p.GetValue(row)) ?? string.Empty))));
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public class OrdersReport(ReportingDbContext db) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/reports/orders");
        Roles("Admin");
        Tags("reports");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var start = Query<DateTime>("startDate", false);
        var end = Query<DateTime>("endDate", false);

        if (start == default) start = new DateTime(2025, 1, 1);
        if (end == default) end = new DateTime(2025, 12, 31);

        var rows = await db.FactOrders
            .Join(db.DimDates, fact => fact.DateKey, date => date.DateKey, (fact, date) => new { fact, date })
            .Join(db.DimCustomers, x => x.fact.CustomerId, customer => customer.CustomerId,
                (x, customer) => new OrdersReportRow(x.fact.OrderNumber, x.date.Date, customer.FullName, x.fact.OrderTotalAmount))
            .Where(x => x.Date >= start && x.Date <= end)
            .ToListAsync(ct);

        if (WantsCsv())
        {
            await Send.StringAsync(CsvFormatter.Format(rows), contentType: "text/csv", cancellation: ct);
            return;
        }

        await Send.OkAsync(new
        {
            Summary = new
            {
                TotalOrders = rows.Select(x => x.OrderNumber).Distinct().Count(),
                Revenue = rows.Sum(x => x.Total),
                AverageOrderValue = rows.Any()
                    ? rows.GroupBy(x => x.OrderNumber).Average(g => g.First().Total)
                    : 0
            },
            Orders = rows
        }, ct);
    }

    private bool WantsCsv() =>
        string.Equals(Query<string>("format", false), "csv", StringComparison.OrdinalIgnoreCase)
        || HttpContext.Request.Headers.Accept.Any(x => x?.Contains("text/csv", StringComparison.OrdinalIgnoreCase) == true);
}

public class ProductSalesReport(ReportingDbContext db) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/reports/product-sales");
        Roles("Admin");
        Tags("reports");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var rows = await db.FactOrders
            .Join(db.DimProducts, fact => fact.ProductId, product => product.ProductId, (fact, product) => new { fact, product })
            .GroupBy(x => new { x.product.ProductId, x.product.ProductName })
            .Select(g => new ProductSalesRow(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Sum(x => x.fact.Quantity),
                g.Sum(x => x.fact.TotalPrice),
                g.Select(x => x.fact.OrderId).Distinct().Count()))
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(ct);

        if (string.Equals(Query<string>("format", false), "csv", StringComparison.OrdinalIgnoreCase))
        {
            await Send.StringAsync(CsvFormatter.Format(rows), contentType: "text/csv", cancellation: ct);
            return;
        }

        await Send.OkAsync(rows, ct);
    }
}

public class CustomerOrdersReport(ReportingDbContext db) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/reports/customers/{customerId}/orders");
        Roles("Admin");
        Tags("reports");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var customerId = Route<int>("customerId");

        var rows = await db.FactOrders
            .Where(x => x.CustomerId == customerId)
            .Join(db.DimDates, fact => fact.DateKey, date => date.DateKey,
                (fact, date) => new CustomerOrdersRow(fact.OrderNumber, date.Date, fact.OrderTotalAmount))
            .Distinct()
            .ToListAsync(ct);

        if (string.Equals(Query<string>("format", false), "csv", StringComparison.OrdinalIgnoreCase))
        {
            await Send.StringAsync(CsvFormatter.Format(rows), contentType: "text/csv", cancellation: ct);
            return;
        }

        await Send.OkAsync(new
        {
            CustomerId = customerId,
            TotalSpent = rows.Sum(x => x.Total),
            Orders = rows
        }, ct);
    }
}
