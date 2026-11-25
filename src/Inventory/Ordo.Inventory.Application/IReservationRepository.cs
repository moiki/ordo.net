using Ordo.Inventory.Domain;

namespace Ordo.Inventory.Application;

public interface IReservationRepository
{
    Task<Reservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
}
