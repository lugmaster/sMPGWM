using System.Reflection;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Inventory;
using sMPGWM.Scripts.UiSystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class InventoryBoundaryTests
{
    [TestCase(typeof(IInventoryView))]
    [TestCase(typeof(IEquipmentView))]
    public void ViewContracts_ExposeReadOnlyStateWithoutMutationOrLifetimeOperations(Type contract)
    {
        Assert.That(typeof(IDisposable).IsAssignableFrom(contract), Is.False);
        Assert.That(contract.GetProperties(), Is.Not.Empty);
        foreach (var property in contract.GetProperties())
            Assert.That(property.SetMethod, Is.Null, property.Name);

        var operations = contract.GetMethods().Where(method => !method.IsSpecialName);
        Assert.That(operations, Is.Empty);
        Assert.That(contract.GetEvents().Select(info => info.Name), Is.EquivalentTo(new[] { "Changed" }));
    }

    [Test]
    public void TransferContract_DoesNotGrantSessionAuthorizationOrLifetimeControl()
    {
        Assert.That(typeof(IDisposable).IsAssignableFrom(typeof(IItemTransfers)), Is.False);
        Assert.That(typeof(IItemTransfers).GetMethods().Select(method => method.Name),
            Is.EquivalentTo(new[] { "Validate", "Transfer" }));
    }

    [Test]
    public void Presentation_DoesNotAcceptOrRetainInventoryAndEquipmentOwners()
    {
        var owners = new[] { typeof(InventoryController), typeof(EquipmentController), typeof(InventoryEquipmentTransferService) };
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var type in typeof(UiCanvas).Assembly.GetTypes())
        {
            if (type.Namespace == null || !type.Namespace.StartsWith("sMPGWM.Scripts.UiSystem", StringComparison.Ordinal))
                continue;

            foreach (var field in type.GetFields(flags))
                Assert.That(owners, Does.Not.Contain(field.FieldType), $"{type.Name}.{field.Name}");
            foreach (var method in type.GetMethods(flags))
                foreach (var parameter in method.GetParameters())
                    Assert.That(owners, Does.Not.Contain(parameter.ParameterType), $"{type.Name}.{method.Name}");
            foreach (var constructor in type.GetConstructors(flags))
                foreach (var parameter in constructor.GetParameters())
                    Assert.That(owners, Does.Not.Contain(parameter.ParameterType), $"{type.Name} constructor");
        }
    }
}
