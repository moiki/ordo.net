using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ordo.BuildingBlocks.Messaging.Abstractions;
using RabbitMQ.Client;

namespace Ordo.BuildingBlocks.Messaging.RabbitMq;

public static class RabbitMqMessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        Action<RabbitMqOptions> configure)
    {
        var options = new RabbitMqOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IBrokerPublisher, RabbitMqBrokerPublisher>();
        services.AddSingleton<RabbitMqBrokerPublisher>();
        services.AddSingleton<IIntegrationEventPublisher, DirectIntegrationEventPublisher>();
        services.AddHostedService<RabbitMqConsumerHost>();
        return services;
    }

    public static IServiceCollection AddRabbitMqConsumer(
        this IServiceCollection services,
        ConsumerSubscription subscription)
    {
        services.AddSingleton(subscription);
        return services;
    }
}
