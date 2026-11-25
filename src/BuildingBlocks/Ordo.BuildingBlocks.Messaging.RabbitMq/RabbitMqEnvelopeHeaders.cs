using System.Globalization;
using System.Text;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using RabbitMQ.Client;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

/// <summary>Maps the broker-neutral envelope to AMQP properties and headers.</summary>
public static class RabbitMqEnvelopeHeaders
{
    private const string OccurredOnUtcHeader = "occurred_on_utc";
    private const string CausationIdHeader = "causation_id";
    private const string TraceParentHeader = "traceparent";
    private const string PartitionKeyHeader = "partition_key";

    public static BasicProperties ToProperties(IntegrationEventEnvelope envelope)
    {
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = envelope.MessageId.ToString(),
            Type = envelope.Type,
            CorrelationId = envelope.CorrelationId.ToString(),
            Headers = new Dictionary<string, object?>()
        };

        properties.Headers![OccurredOnUtcHeader] = envelope.OccurredOnUtc.ToString("O", CultureInfo.InvariantCulture);
        properties.Headers[PartitionKeyHeader] = envelope.PartitionKey;

        if (envelope.CausationId.HasValue)
        {
            properties.Headers[CausationIdHeader] = envelope.CausationId.Value.ToString();
        }

        if (envelope.TraceParent is not null)
        {
            properties.Headers[TraceParentHeader] = envelope.TraceParent;
        }

        return properties;
    }

    public static IntegrationEventEnvelope FromProperties(IReadOnlyBasicProperties properties)
    {
        string? type = properties.Type;
        string? messageId = properties.MessageId;
        string? correlationId = properties.CorrelationId;
        string? occurredOnUtc = ReadHeader(properties, OccurredOnUtcHeader);
        string? partitionKey = ReadHeader(properties, PartitionKeyHeader);

        if (type is null || messageId is null || correlationId is null || occurredOnUtc is null || partitionKey is null)
        {
            throw new InvalidDataException("Message headers are incomplete.");
        }

        if (!Guid.TryParse(messageId, out Guid parsedMessageId)
            || !Guid.TryParse(correlationId, out Guid parsedCorrelationId))
        {
            throw new InvalidDataException("Message id or correlation id is not a GUID.");
        }

        if (!DateTimeOffset.TryParse(occurredOnUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsedOccurredOn))
        {
            throw new InvalidDataException("occurred_on_utc is not a timestamp.");
        }

        Guid? causationId = ReadHeader(properties, CausationIdHeader) is { } causation && Guid.TryParse(causation, out Guid parsedCausation)
            ? parsedCausation
            : null;

        return new IntegrationEventEnvelope(
            parsedMessageId,
            type,
            parsedOccurredOn,
            parsedCorrelationId,
            causationId,
            ReadHeader(properties, TraceParentHeader),
            partitionKey);
    }

    private static string? ReadHeader(IReadOnlyBasicProperties properties, string name)
    {
        if (properties.Headers is null
            || !properties.Headers.TryGetValue(name, out object? value)
            || value is null)
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value.ToString()
        };
    }
}
