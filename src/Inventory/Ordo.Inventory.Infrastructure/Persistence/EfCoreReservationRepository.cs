using Microsoft.EntityFrameworkCore;
using Ordo.Inventory.Application;
using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Infrastructure.Persistence;

internal sealed class EfCoreReservationRepository(InventoryDbContext context) : IReservationRepository
{
    public Task<Reservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        context.Reservations.SingleOrDefaultAsync(reservation => reservation.OrderId == orderId, cancellationToken);

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken) =>
        await context.Reservations.AddAsync(reservation, cancellationToken);
}
