using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class ItemDisplayTests
{
    [TestCase(0L, "0")]
    [TestCase(1L, "1")]
    [TestCase(999L, "999")]
    [TestCase(1000L, "1K")]
    [TestCase(1199L, "1.1K")]
    [TestCase(1200L, "1.2K")]
    [TestCase(999999L, "999.9K")]
    [TestCase(1000000L, "1M")]
    [TestCase(2147483647L, "2.1B")]
    [TestCase(4294967294L, "4.2B")]
    [TestCase(1000000000000L, "1T")]
    [TestCase(1000000000000000L, "1Q")]
    [TestCase(long.MaxValue, "9.2E")]
    public void Quantity_AbbreviatesWithoutRoundingUp(long quantity, string expected) =>
        Assert.That(IconDisplayData.FormatQuantity(quantity), Is.EqualTo(expected));

    [Test]
    public void NegativeQuantity_IsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => IconDisplayData.FormatQuantity(-1));

    [Test]
    public void WeaponTooltipsShowOnlyTheirTypedSupplyFields()
    {
        var categories = ItemTestData.ParseCategories(
            GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var definitions = ItemTestData.ParseItems(
            GameplayTestData.Read("ItemDefinitions.yml", "items/ItemSchema.yml"), categories, _ => null!);
        var ammunition = ItemDisplayFormatter.Create(new ItemEntity(ItemEntityId.FromNetwork(1), definitions["vulcan_mk1"], 1));
        var energy = ItemDisplayFormatter.Create(new ItemEntity(ItemEntityId.FromNetwork(2), definitions["laser_mk1"], 1));

        Assert.That(ammunition.Tooltip, Does.Contain("\n" + definitions["vulcan_mk1"].Description + "\n"));
        Assert.That(energy.Tooltip, Does.Contain("\n" + definitions["laser_mk1"].Description + "\n"));

        Assert.That(ammunition.Tooltip, Does.Contain("Mass: 1").And.Contain("Supply: Ammunition")
            .And.Contain("AmmoCapacity: 60").And.Contain("ReloadDuration: 1500 ms")
            .And.Contain("EnergyConsumption: 1 /s").And.Not.Contain("EnergyPerShot:"));
        Assert.That(energy.Tooltip, Does.Contain("Mass: 1").And.Contain("Supply: Energy")
            .And.Contain("EnergyPerShot: 4").And.Contain("SpreadDegrees: 0 degrees")
            .And.Contain("EnergyConsumption: 2 /s").And.Not.Contain("AmmoCapacity:").And.Not.Contain("ReloadDuration:"));
    }

    [TestCase("mock_standard_resource")]
    [TestCase("mock_standard_consumable")]
    public void StackableItemTooltipsShowPerItemMassAndQuantity(string id)
    {
        var categories = ItemTestData.ParseCategories(
            GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var definitions = ItemTestData.ParseItems(
            GameplayTestData.Read("ItemDefinitions.yml", "items/ItemSchema.yml"), categories, _ => null!);
        var display = ItemDisplayFormatter.Create(new ItemEntity(ItemEntityId.FromNetwork(1), definitions[id], 3));

        Assert.That(display.Quantity, Is.EqualTo(3));
        Assert.That(display.Tooltip, Does.Contain("Mass: 1").And.Contain("Quantity: 3").And.Not.Contain("Supply:"));
    }

    [Test]
    [Category("Godot")]
    public void Tooltip_PreservesExactAggregateQuantityBeyondIndividualStackLimit()
    {
        var definition = GameplayDefinitions.Current.Items.GetById("mock_advanced_resource");
        var display = ItemDisplayFormatter.Create(definition.Id, int.MaxValue);
        Assert.That(display.Tooltip, Does.Contain("2147483647"));
    }

    [TestCase(0L)]
    [TestCase(4294967294L)]
    [Category("Godot")]
    public void HotbarTooltipSupportsZeroAndAggregateQuantities(long quantity)
    {
        var definition = GameplayDefinitions.Current.Items.GetById("mock_standard_consumable");
        var display = ItemDisplayFormatter.Create( definition.Id, quantity);
        Assert.That(display.Quantity, Is.EqualTo(quantity));
        Assert.That(display.Tooltip, Does.Contain($"Quantity: {quantity}").And.Contain("Stack limit: 10"));
    }
}
