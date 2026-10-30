using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.Items;
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
    [Category("Godot")]
    public void Tooltip_PreservesExactAggregateQuantityBeyondIndividualStackLimit()
    {
        var definition = ItemRegistry.GetById("mock_advanced_resource");
        var display = ItemDisplayFormatter.Create(definition.Id, int.MaxValue);
        Assert.That(display.Tooltip, Does.Contain("2147483647"));
    }

    [TestCase(0L)]
    [TestCase(4294967294L)]
    [Category("Godot")]
    public void HotbarTooltipSupportsZeroAndAggregateQuantities(long quantity)
    {
        var definition = ItemRegistry.GetById("mock_standard_consumable");
        var display = ItemDisplayFormatter.Create( definition.Id, quantity);
        Assert.That(display.Quantity, Is.EqualTo(quantity));
        Assert.That(display.Tooltip, Does.Contain($"Quantity: {quantity}").And.Contain("Stack limit: 10"));
    }
}
