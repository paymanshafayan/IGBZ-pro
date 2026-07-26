using IGBZ.Application.Catalog;
using IGBZ.Application.Orders;

namespace IGBZ.Api.Endpoints;

public static class StorefrontEndpoints
{
    public static IEndpointRouteBuilder MapStorefrontEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/storefront").WithTags("Storefront");

        group.MapGet("/products", async (CatalogService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListPublishedProductsAsync(cancellationToken)));

        group.MapGet("/products/{slug}", async (string slug, CatalogService service, CancellationToken cancellationToken)
            => Results.Ok(await service.GetPublishedProductBySlugAsync(slug, cancellationToken)));

        group.MapPost("/checkout/orders", async (CreateOrderRequest request, OrderService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/storefront/orders", await service.CreateOrderAsync(request, cancellationToken)));

        group.MapGet("/orders/{id}", async (string id, OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.GetOrderAsync(id, cancellationToken)));

        return endpoints;
    }
}
