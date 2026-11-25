using System.Reflection;
using Mono.Cecil;
using NetArchTest.Rules;
using Shouldly;

namespace Ordo.ArchitectureTests;

public sealed class ArchitectureRulesTests
{
    private static readonly Assembly ContractsAssembly = typeof(Ordo.Contracts.IIntegrationEvent).Assembly;
    private static readonly Assembly MessagingAbstractionsAssembly = typeof(Ordo.BuildingBlocks.Messaging.Abstractions.IBrokerPublisher).Assembly;
    private static readonly Assembly RabbitMqAssembly = typeof(Ordo.BuildingBlocks.Messaging.RabbitMq.RabbitMqBrokerPublisher).Assembly;
    private static readonly Assembly EfCoreAssembly = typeof(Ordo.BuildingBlocks.Messaging.EfCore.OutboxMessage).Assembly;
    private static readonly Assembly OrdersDomainAssembly = typeof(Ordo.Orders.Domain.AssemblyMarker).Assembly;
    private static readonly Assembly OrdersApplicationAssembly = typeof(Ordo.Orders.Application.AssemblyMarker).Assembly;
    private static readonly Assembly OrdersInfrastructureAssembly = typeof(Ordo.Orders.Infrastructure.AssemblyMarker).Assembly;
    private static readonly Assembly OrdersApiAssembly = typeof(Ordo.Orders.Api.AssemblyMarker).Assembly;
    private static readonly Assembly InventoryDomainAssembly = typeof(Ordo.Inventory.Domain.AssemblyMarker).Assembly;
    private static readonly Assembly InventoryApplicationAssembly = typeof(Ordo.Inventory.Application.AssemblyMarker).Assembly;
    private static readonly Assembly InventoryInfrastructureAssembly = typeof(Ordo.Inventory.Infrastructure.AssemblyMarker).Assembly;
    private static readonly Assembly InventoryApiAssembly = typeof(Ordo.Inventory.Api.AssemblyMarker).Assembly;

    [Fact]
    public void Domain_assemblies_should_not_depend_on_other_ordo_assemblies()
    {
        AssertNoForbiddenDependencies(OrdersDomainAssembly, ["Ordo."]);
        AssertNoForbiddenDependencies(InventoryDomainAssembly, ["Ordo."]);
    }

    [Fact]
    public void Application_assemblies_should_not_depend_on_infrastructure_frameworks()
    {
        string[] forbidden =
        [
            "Microsoft.EntityFrameworkCore",
            "RabbitMQ.Client",
            "Microsoft.AspNetCore"
        ];

        AssertNoForbiddenDependencies(OrdersApplicationAssembly, forbidden);
        AssertNoForbiddenDependencies(InventoryApplicationAssembly, forbidden);
    }

    [Fact]
    public void Services_should_not_reference_other_services()
    {
        AssertNoForbiddenDependencies(OrdersDomainAssembly, ["Ordo.Inventory"]);
        AssertNoForbiddenDependencies(OrdersApplicationAssembly, ["Ordo.Inventory"]);
        AssertNoForbiddenDependencies(OrdersInfrastructureAssembly, ["Ordo.Inventory"]);
        AssertNoForbiddenDependencies(OrdersApiAssembly, ["Ordo.Inventory"]);

        AssertNoForbiddenDependencies(InventoryDomainAssembly, ["Ordo.Orders"]);
        AssertNoForbiddenDependencies(InventoryApplicationAssembly, ["Ordo.Orders"]);
        AssertNoForbiddenDependencies(InventoryInfrastructureAssembly, ["Ordo.Orders"]);
        AssertNoForbiddenDependencies(InventoryApiAssembly, ["Ordo.Orders"]);
    }

    [Fact]
    public void Contracts_should_only_contain_records_or_enums()
    {
        Types.InAssembly(ContractsAssembly)
            .That()
            .AreNotInterfaces()
            .And()
            .AreNotAbstract()
            .Should()
            .MeetCustomRule(new RecordOrEnumRule())
            .GetResult()
            .IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Integration_events_should_implement_contract_and_versioned_naming()
    {
        TestResult result = Types.InAssembly(ContractsAssembly)
            .That()
            .ImplementInterface(typeof(Ordo.Contracts.IIntegrationEvent))
            .Should()
            .BeClasses()
            .And()
            .BeSealed()
            .And()
            .HaveNameEndingWith("V1", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Building_blocks_should_remain_technical()
    {
        string[] domainTerms = ["Order", "Payment", "Inventory", "Notification", "Customer"];
        AssertNoForbiddenDependencies(MessagingAbstractionsAssembly, domainTerms);
        AssertNoForbiddenDependencies(RabbitMqAssembly, domainTerms);
        AssertNoForbiddenDependencies(EfCoreAssembly, domainTerms);
    }

    private static void AssertNoForbiddenDependencies(Assembly assembly, IEnumerable<string> forbiddenDependencies)
    {
        string[] forbidden = forbiddenDependencies.ToArray();
        string[] referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        referencedAssemblies.Any(name => forbidden.Any(name.Contains)).ShouldBeFalse(
            $"{assembly.GetName().Name} references forbidden assemblies: {string.Join(", ", referencedAssemblies.Where(name => forbidden.Any(name.Contains)))}");

        TestResult result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    private sealed class RecordOrEnumRule : ICustomRule
    {
        public bool MeetsRule(TypeDefinition type)
        {
            return type.IsEnum || type.Methods.Any(method => method.Name == "<Clone>$");
        }
    }
}
