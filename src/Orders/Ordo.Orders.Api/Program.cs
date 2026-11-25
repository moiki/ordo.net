using Microsoft.EntityFrameworkCore;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using Ordo.BuildingBlocks.Messaging.RabbitMq;
using Ordo.Contracts.Inventory;
using Ordo.Orders.Application;
using Ordo.Orders.Infrastructure;
using Ordo.Orders.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string ordersConnectionString = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("ConnectionStrings:Orders is not configured.");
string rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq is not configured.");

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddOrdersInfrastructure(ordersConnectionString);
builder.Services.AddRabbitMqMessaging(options => options.ConnectionString = rabbitMqConnectionString);
builder.Services.AddSingleton(TimeProvider.System);

// Hand-written handler registrations: endpoints and consumers call handlers through their interfaces.
// Decorators (ADR 0005) wrap these same interfaces; no mediator is involved.
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<ICommandHandler<CreateOrderCommand, Result<Guid>>>(services =>
    services.GetRequiredService<CreateOrderHandler>());
builder.Services.AddScoped<GetOrderHandler>();
builder.Services.AddScoped<IQueryHandler<GetOrderQuery, Result<OrderDto>>>(services =>
    services.GetRequiredService<GetOrderHandler>());
builder.Services.AddScoped<StockReservedHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<StockReservedV1>>(services =>
    services.GetRequiredService<StockReservedHandler>());
builder.Services.AddScoped<StockRejectedHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<StockRejectedV1>>(services =>
    services.GetRequiredService<StockRejectedHandler>());

builder.Services.AddRabbitMqConsumer(
    ConsumerSubscription.For<StockReservedV1>("orders.on-stock-reserved", "inventory.stock-reserved.v1"));
builder.Services.AddRabbitMqConsumer(
    ConsumerSubscription.For<StockRejectedV1>("orders.on-stock-rejected", "inventory.stock-rejected.v1"));

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { status = "Alive" }));
app.MapGet("/health/ready", async Task<IResult> (OrdersDbContext context, CancellationToken cancellationToken) =>
    await context.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "Ready" })
        : Results.Problem(title: "Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable));

app.MapPost("/orders", async Task<IResult> (
    CreateOrderRequest request,
    ICommandHandler<CreateOrderCommand, Result<Guid>> createHandler,
    IQueryHandler<GetOrderQuery, Result<OrderDto>> getHandler,
    CancellationToken cancellationToken) =>
{
    Result<Guid> created = await createHandler.HandleAsync(
        new CreateOrderCommand(request.CustomerId, request.Currency, request.Lines),
        cancellationToken);

    if (!created.IsSuccess)
    {
        return Results.Problem(
            title: "Invalid order",
            detail: created.Error!.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }

    Guid orderId = created.Value;
    Result<OrderDto> order = await getHandler.HandleAsync(new GetOrderQuery(orderId), cancellationToken);
    return Results.Created($"/orders/{orderId}", order.Value);
});

app.MapGet("/orders/{orderId:guid}", async Task<IResult> (
    Guid orderId,
    IQueryHandler<GetOrderQuery, Result<OrderDto>> handler,
    CancellationToken cancellationToken) =>
{
    Result<OrderDto> result = await handler.HandleAsync(new GetOrderQuery(orderId), cancellationToken);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.Problem(title: "Order not found", detail: result.Error!.Message, statusCode: StatusCodes.Status404NotFound);
});

await app.RunAsync();

// HTTP-only request shape. It never escapes into Application or Domain.
public sealed record CreateOrderRequest(
    Guid CustomerId,
    string Currency,
    IReadOnlyList<CreateOrderLine> Lines);
