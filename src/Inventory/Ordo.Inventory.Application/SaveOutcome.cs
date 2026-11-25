namespace Ordo.Inventory.Application;

/// <summary>Infrastructure translates a row-version conflict into this Application-level outcome.</summary>
public enum SaveOutcome
{
    Success,
    ConcurrencyConflict
}
