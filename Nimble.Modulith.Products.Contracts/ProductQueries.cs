using Mediator; namespace Nimble.Modulith.Products.Contracts;
public record GetProductPriceQuery(int ProductId):IQuery<decimal>;
public record GetProductDetailsQuery(int ProductId):IQuery<ProductDetailsResult>;
public record ProductDetailsResult(int Id,string Name,decimal Price);
