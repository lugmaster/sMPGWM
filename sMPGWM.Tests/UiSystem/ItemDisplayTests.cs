using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.Util.UiComponents;

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
    public void Tooltip_UsesExactQuantityAndConfiguredLimit()
    {
        var definition = new ItemDefinition(new ItemDefinitionId("limited"), "Repair", ItemCategories.Consumable,
            "Repairs a ship.", maximumStackSize: 10);
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[] { definition });
        var display = ItemDisplayFormatter.Create(catalog, catalog.CreateEntity(definition.Id));
        Assert.That(display.Quantity, Is.EqualTo(1));
        Assert.That(display.Tooltip, Is.EqualTo("Repair\nConsumable\nRepairs a ship.\nQuantity: 1\nStack limit: 10"));
    }

    [Test]
    public void UnlimitedTooltip_PreservesExactLargeQuantity()
    {
        var definition = new ItemDefinition(new ItemDefinitionId("unlimited"), "Ore", ItemCategories.Resource);
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[] { definition });
        var display = ItemDisplayFormatter.Create(catalog, catalog.CreateEntity(definition.Id, int.MaxValue));
        Assert.That(display.Tooltip, Does.Contain("2147483647").And.Contain("No configured limit"));
    }

    [TestCase(0L)]
    [TestCase(4294967294L)]
    public void HotbarTooltipSupportsZeroAndAggregateQuantities(long quantity)
    {
        var definition = new ItemDefinition(new ItemDefinitionId("linked"), "Repair", ItemCategories.Consumable,
            maximumStackSize: 10);
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[] { definition });
        var display = ItemDisplayFormatter.Create(catalog, definition.Id, quantity);
        Assert.That(display.Quantity, Is.EqualTo(quantity));
        Assert.That(display.Tooltip, Does.Contain($"Quantity: {quantity}").And.Contain("Stack limit: 10"));
    }

    [Test]
    public void EmptyAndNonstackableDisplays_HaveNoQuantity()
    {
        var definition = new ItemDefinition(new ItemDefinitionId("engine"), "Engine", ItemCategories.Engine);
        var catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[] { definition });
        var display = ItemDisplayFormatter.Create(catalog, catalog.CreateEntity(definition.Id));
        Assert.That(display.Quantity, Is.Null);
        Assert.That(display.Tooltip, Does.Not.Contain("Quantity").And.Not.Contain("Stack limit"));
        Assert.That(ItemDisplayFormatter.Create(catalog, null), Is.EqualTo(new IconDisplayData()));
    }
}
