using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

public sealed class EquipmentDefinitionValidationTests
{
    [Test]
    public void PackagedShipAndItemCombinationsAreValid()
    {
        Assert.DoesNotThrow(() => EquipmentDefinitionValidator.Validate(Ships(), Items()));
    }

    [Test]
    public void IndividuallyValidMovementCannotOverflowWithShipModifier()
    {
        var items = Items();
        for (var i = 0; i < items.Length; i++)
            if (items[i] is EngineItemDefinition engine) items[i] = engine with { Speed = float.MaxValue };
        var document = GameplayTestData.Read("Ships.yml", "ShipsSchema.yml");
        foreach (var ship in document["ships"]!) ship["stat_modifiers"]!["Speed"] = float.MaxValue;
        var ships = ShipCatalogueLoader.Load(_ => document, _ => true).All;
        var error = Assert.Catch<ArgumentException>(() => EquipmentDefinitionValidator.Validate(ships, items));
        Assert.That(error!.Message, Does.Contain("ship").And.Contain("item").And.Contain("Movement"));
    }

    [Test]
    public void IndividuallyValidComponentsCannotExceedCombinedMassLimit()
    {
        var items = Items();
        for (var i = 0; i < items.Length; i++)
            if (items[i] is EngineItemDefinition or HullItemDefinition)
                items[i] = items[i] with { Mass = (double)float.MaxValue / 3 };
        var error = Assert.Catch<ArgumentException>(() => EquipmentDefinitionValidator.Validate(Ships(), items));
        Assert.That(error!.Message, Does.Contain("combined equipment mass"));
    }

    [Test]
    public void RepeatedWeaponsRespectFixtureBudgetWhenCheckingMass()
    {
        var items = Items();
        for (var i = 0; i < items.Length; i++)
            if (items[i] is WeaponItemDefinition) items[i] = items[i] with { Mass = (double)float.MaxValue / 10 };
        var error = Assert.Catch<ArgumentException>(() => EquipmentDefinitionValidator.Validate(Ships(), items));
        Assert.That(error!.Message, Does.Contain("combined equipment mass with fixture"));
        for (var i = 0; i < items.Length; i++)
            if (items[i] is WeaponFixtureItemDefinition fixture) items[i] = fixture with { WeaponSlots = 1 };
        Assert.DoesNotThrow(() => EquipmentDefinitionValidator.Validate(Ships(), items));
    }

    [Test]
    public void IndividuallyValidEnergyDemandsCannotOverflowTogether()
    {
        var items = Items();
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] is EngineItemDefinition engine) items[i] = engine with { EnergyConsumption = double.MaxValue / 2 };
            if (items[i] is BatteryItemDefinition battery) items[i] = battery with { EnergyConsumption = double.MaxValue / 2 };
        }
        var error = Assert.Catch<ArgumentException>(() => EquipmentDefinitionValidator.Validate(Ships(), items));
        Assert.That(error!.Message, Does.Contain("combined energy demand with afterburner"));
    }

    [TestCase(-1)]
    [TestCase(10)]
    public void FixtureRangeIsEnforcedWhenConstructingOrCopying(int slots)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponFixtureItemDefinition("test", "test", "test", "", false, 1, null!)
            { Mass = 0, WeaponSlots = slots });
        var fixture = new WeaponFixtureItemDefinition("test", "test", "test", "", false, 1, null!)
            { Mass = 0, WeaponSlots = 9 };
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = fixture with { WeaponSlots = slots });
        Assert.That(WeaponSlotPolicy.GetCapacity(fixture), Is.EqualTo(9));
    }

    private static ItemDefinition[] Items()
    {
        var categories = ItemTestData.ParseCategories(
            GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        var items = ItemTestData.ParseItems(GameplayTestData.Read("ItemDefinitions.yml", "items/ItemSchema.yml"), categories, _ => null!);
        var result = new ItemDefinition[items.Count];
        items.Values.CopyTo(result, 0);
        return result;
    }

    private static ShipData[] Ships()
    {
        var ships = ShipCatalogueLoader.Load(_ => GameplayTestData.Read("Ships.yml", "ShipsSchema.yml"), _ => true).All;
        var result = new ShipData[ships.Count];
        var index = 0;
        foreach (var ship in ships) result[index++] = ship;
        return result;
    }
}
