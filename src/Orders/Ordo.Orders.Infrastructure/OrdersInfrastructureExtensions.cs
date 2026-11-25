using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordo.Orders.Application;
using Ordo.Orders.Infrastructure.Persistence;

namespace Ordo.Orders.Infrastructure;

public static class OrdersInfrastructureExtensions
{
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services,
        string connectionString) =>
        services
            .AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString))
            .AddScoped<IOrderRepository, EfCoreOrderRepository>()
            .AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
}
