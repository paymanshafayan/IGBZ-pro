using IGBZ.Application.Catalog;
using IGBZ.Application.Integrations;
using IGBZ.Application.Inventory;
using IGBZ.Application.Orders;
using IGBZ.Application.Pricing;

namespace IGBZ.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin").WithTags("Admin").RequireAuthorization("TenantAdmin");

        group.MapPost("/categories", async (CreateCategoryRequest request, CatalogService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/admin/categories", await service.CreateCategoryAsync(request, cancellationToken)));

        group.MapGet("/categories", async (CatalogService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListCategoriesAsync(cancellationToken)));

        group.MapPost("/products", async (CreateProductRequest request, CatalogService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/admin/products", await service.CreateProductAsync(request, cancellationToken)));

        group.MapGet("/products", async (CatalogService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListAdminProductsAsync(cancellationToken)));

        group.MapGet("/products/{id}", async (string id, CatalogService service, CancellationToken cancellationToken)
            => Results.Ok(await service.GetProductByIdAsync(id, cancellationToken)));

        group.MapPost("/products/{id}/publish", async (string id, CatalogService service, CancellationToken cancellationToken)
            => Results.Ok(await service.PublishProductAsync(id, cancellationToken)));

        group.MapPost("/discounts", async (CreateDiscountRequest request, DiscountService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/admin/discounts", await service.CreateAsync(request, cancellationToken)));

        group.MapGet("/discounts", async (DiscountService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListAsync(cancellationToken)));

        group.MapPost("/integrations", async (CreateIntegrationConnectionRequest request, IntegrationService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/admin/integrations", await service.CreateConnectionAsync(request, cancellationToken)));

        group.MapGet("/integrations", async (IntegrationService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListConnectionsAsync(cancellationToken)));

        group.MapPost("/integrations/{connectionId}/sync-jobs", async (string connectionId, QueueIntegrationSyncRequest request, IntegrationService service, CancellationToken cancellationToken)
            => Results.Created("/api/v1/admin/integrations/sync-jobs", await service.QueueSyncAsync(connectionId, request, cancellationToken)));

        group.MapGet("/integrations/sync-jobs", async (IntegrationService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListJobsAsync(cancellationToken)));

        group.MapGet("/inventory", async (InventoryApplicationService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListAsync(cancellationToken)));

        group.MapPost("/inventory/{id}/adjust", async (string id, AdjustInventoryRequest request, InventoryApplicationService service, CancellationToken cancellationToken)
            => Results.Ok(await service.AdjustAsync(id, request, cancellationToken)));

        group.MapGet("/orders", async (OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ListOrdersAsync(cancellationToken)));

        group.MapGet("/orders/{id}", async (string id, OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.GetOrderAsync(id, cancellationToken)));

        group.MapPost("/orders/{id}/processing", async (string id, OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.StartProcessingAsync(id, cancellationToken)));

        group.MapPost("/orders/{id}/shipped", async (string id, OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.MarkAsShippedAsync(id, cancellationToken)));

        group.MapPost("/orders/{id}/delivered", async (string id, OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.MarkAsDeliveredAsync(id, cancellationToken)));

        group.MapPost("/orders/{id}/cancel", async (string id, OrderActionRequest request, OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.CancelAsync(id, request.Reason ?? "Admin cancellation", cancellationToken)));

        group.MapPost("/orders/{id}/return", async (string id, OrderActionRequest request, OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.ReturnAsync(id, request.Reason ?? "Admin return", cancellationToken)));

        group.MapPost("/orders/{id}/refund", async (string id, OrderActionRequest request, OrderService service, CancellationToken cancellationToken)
            => Results.Ok(await service.RefundAsync(id, request.Reason ?? "Admin refund", cancellationToken)));

        return endpoints;
    }
}
