using Mediator; namespace Nimble.Modulith.Customers.Contracts;
public record OrderCreatedEvent(int OrderId,string OrderNumber,DateOnly OrderDate,int CustomerId,string CustomerEmail,string CustomerName,decimal OrderTotal,IReadOnlyList<OrderCreatedItem> Items):INotification;
public record OrderCreatedItem(int OrderItemId,int ProductId,string ProductName,int Quantity,decimal UnitPrice,decimal TotalPrice);
