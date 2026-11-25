namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";

    public string ExchangeName { get; set; } = "ordo.events";

    public ushort PrefetchCount { get; set; } = 16;
}
