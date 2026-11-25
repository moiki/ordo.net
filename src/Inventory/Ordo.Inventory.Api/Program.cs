using Microsoft.EntityFrameworkCore;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.BuildingBlocks.Messaging.RabbitMq;
using Ordo.Contracts.Orders;
using Ordo.Inventory.Application;
using Ordo.Inventory.Infrastructure;
using Ordo.Inventory.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string inventoryConnectionString = builder.Configuration.GetConnectionString("Inventory")
    ?? throw new InvalidOperationException("ConnectionStrings:Inventory is not configured.");
string rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq is not configured.");

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddInventoryInfrastructure(inventoryConnectionString);
builder.Services.AddRabbitMqMessaging(options => options.ConnectionString = rabbitMqConnectionString);

// Hand-written registrations; endpoints and consumers call handlers through their interfaces (ADR 0005).
builder.Services.AddScoped<SetStockHandler>();
builder.Services.AddScoped<ICommandHandler<SetStockCommand, Result<StockDto>>>(services =>
    services.GetRequiredService<SetStockHandler>());
builder.Services.AddScoped<OrderCreatedHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedV1>>(services =>
    services.GetRequiredService<OrderCreatedHandler>());

builder.Services.AddRabbitMqConsumer(
    ConsumerSubscription.For<OrderCreatedV1>("inventory.on-order-created", "orders.order-created.v1"));

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }));
app.MapGet("/health/ready", async Task<IResult> (InventoryDbContext context, CancellationToken cancellationToken) =>
    await context.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "Ready" })
        : Results.Problem(title: "Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable));

// Development-only admin endpoint: no authentication exists until a later phase.
app.MapPut("/inventory/stock/{productId:guid}", async Task<IResult> (
    Guid productId,
    SetStockRequest request,
    ICommandHandler<SetStockCommand, Result<StockDto>> handler,
    CancellationToken cancellationToken) =>
{
    Result<StockDto> result = await handler.HandleAsync(
        new SetStockCommand(productId, request.Quantity),
        cancellationToken);

    if (!result.IsSuccess)
    {
        bool conflict = result.Error!.Code == "stock.conflict";
        return Results.Problem(
            title: conflict ? "Concurrent stock update" : "Invalid stock request",
            detail: result.Error.Message,
            statusCode: conflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest);
    }

    return Results.Ok(result.Value);
});

await app.RunAsync();

// HTTP-only request shape. It never escapes into Application or Domain.
public sealed record SetStockRequest(int Quantity);
