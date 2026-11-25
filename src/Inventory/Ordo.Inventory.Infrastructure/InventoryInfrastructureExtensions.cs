using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordo.Inventory.Application;
using Ordo.Inventory.Infrastructure.Persistence;

namespace Ordo.Inventory.Infrastructure;

public static class InventoryInfrastructureExtensions
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        string connectionString) =>
        services
            .AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString))
            .AddScoped<IStockItemRepository, EfCoreStockItemRepository>()
            .AddScoped<IReservationRepository, EfCoreReservationRepository>()
            .AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
}
