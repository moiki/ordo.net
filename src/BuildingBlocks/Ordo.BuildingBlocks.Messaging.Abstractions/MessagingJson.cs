using System.Text.Json;
using System.Text.Json.Serialization;
using Ordo.Contracts;

namespace Ordo.BuildingBlocks.Messaging.Abstractions;

/// <summary>The single shared serializer options instance for event bodies.</summary>
public static class MessagingJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(IIntegrationEvent integrationEvent) =>
        JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);

    public static TEvent? Deserialize<TEvent>(string json)
        where TEvent : IIntegrationEvent =>
        JsonSerializer.Deserialize<TEvent>(json, Options);
}
