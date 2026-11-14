using System.Collections;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.ItemSystem;

public sealed class ItemDefinitionSchemaTests
{
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void AmmunitionConstructorRequiresPositiveCapacity(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AmmunitionSupplyDefinition(capacity, 1500));
    }

    [TestCase(0d)]
    [TestCase(-1d)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void SupplyConstructorsRequirePositiveFiniteAmounts(double amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AmmunitionSupplyDefinition(60, amount));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnergySupplyDefinition(amount));
    }

    [Test]
    public void SupplyConstructorsRetainTypedConfiguration()
    {
        var ammunition = new AmmunitionSupplyDefinition(60, 1500);
        var energy = new EnergySupplyDefinition(4);
        Assert.That(ammunition.AmmoCapacity, Is.EqualTo(60));
        Assert.That(ammunition.ReloadDurationMs, Is.EqualTo(1500));
        Assert.That(energy.EnergyPerShot, Is.EqualTo(4));
    }

    [Test]
    public void IntegralNumericTokensAgreeBetweenSchemasAndParser()
    {
        var weaponDocument = WeaponDocument();
        weaponDocument["Items"]![0]!["Supply"]!["AmmoCapacity"] = 60d;
        AssertSchemaAndParser(weaponDocument, "light_weapon", true);
        var weapon = (WeaponItemDefinition)Parse(weaponDocument)["vulcan_mk1"];
        Assert.That(((AmmunitionSupplyDefinition)weapon.Supply).AmmoCapacity, Is.EqualTo(60));

        var fixtureDocument = GameplayTestData.Read("items/weapon_fixture.yml", "items/weapon_fixture.schema.yml");
        fixtureDocument["Items"]![0]!["WeaponSlots"] = 9d;
        AssertSchemaAndParser(fixtureDocument, "weapon_fixture", true);
        var fixtureId = (string)fixtureDocument["Items"]![0]!["Id"]!;
        Assert.That(((WeaponFixtureItemDefinition)Parse(fixtureDocument)[fixtureId]).WeaponSlots, Is.EqualTo(9));

        var resourceDocument = GameplayTestData.Read("items/resource.yml", "items/resource.schema.yml");
        var resource = (JObject)resourceDocument["Items"]![0]!;
        resource.Remove("CanStack");
        resource["StackLimit"] = 2d;
        AssertSchemaAndParser(resourceDocument, "resource", true);
        Assert.That(Parse(resourceDocument)[(string)resource["Id"]!].StackLimit, Is.EqualTo(2));
    }

    [Test]
    public void AmmunitionCapacityBeyondRuntimeIntegerRangeIsRejected()
    {
        var document = WeaponDocument();
        document["Items"]![0]!["Supply"]!["AmmoCapacity"] = (long)int.MaxValue + 1;
        AssertSchemaAndParser(document, "light_weapon", false);
    }

    [Test]
    public void CompleteCatalogueUsesTypedDefinitionsAndPreservesExistingWeapons()
    {
        var document = GameplayTestData.Read("ItemDefinitions.yml", "items/ItemSchema.yml");
        AssertSchema(document, "items/ItemSchema.yml", true);
        var definitions = Parse(document);
        foreach (var category in new[] { "light_weapon", "medium_weapon", "heavy_weapon" })
        foreach (var variant in new[] { "standard", "advanced" })
        {
            var id = category == "light_weapon" ? (variant == "standard" ? "vulcan_mk1" : "laser_mk1") : $"mock_{variant}_{category}";
            Assert.That(definitions[id], Is.TypeOf<WeaponItemDefinition>(), id);
            Assert.That(definitions[id].CategoryId, Is.EqualTo(category));
        }

        foreach (var definition in definitions.Values)
        {
            Assert.That(definition.GetType(), Is.Not.EqualTo(typeof(ItemDefinition)), definition.Id);
            Assert.That(definition.Mass, Is.GreaterThanOrEqualTo(0), definition.Id);
            Assert.That(double.IsFinite(definition.Mass), Is.True, definition.Id);
            AssertNoDictionaryProperties(definition.GetType());
            if (definition is WeaponItemDefinition weapon)
            {
                Assert.That(weapon.Supply, Is.InstanceOf<WeaponSupplyDefinition>(), definition.Id);
                AssertNoDictionaryProperties(weapon.Supply.GetType());
            }
        }

        var vulcan = (WeaponItemDefinition)definitions["vulcan_mk1"];
        var laser = (WeaponItemDefinition)definitions["laser_mk1"];
        Assert.That(vulcan.Supply, Is.EqualTo(new AmmunitionSupplyDefinition(60, 1500)));
        Assert.That(laser.Supply, Is.EqualTo(new EnergySupplyDefinition(4)));
        Assert.That(vulcan.Damage, Is.EqualTo(1.8));
        Assert.That(vulcan.SpreadDegrees, Is.EqualTo(6));
        Assert.That(laser.Damage, Is.EqualTo(9));
        Assert.That(laser.SpreadDegrees, Is.Zero);
        var row = GameplayTestData.Find((JArray)document["Items"]!, "vulcan_mk1");
        row["Damage"] = 500;
        row["Supply"]!["AmmoCapacity"] = 100;
        Assert.That(vulcan.Damage, Is.EqualTo(1.8));
        Assert.That(((AmmunitionSupplyDefinition)vulcan.Supply).AmmoCapacity, Is.EqualTo(60));
    }

    [TestCase("Ammunition", true, false, true, true)]
    [TestCase("Ammunition", false, false, true, false)]
    [TestCase("Ammunition", true, true, true, false)]
    [TestCase("Ammunition", false, true, true, false)]
    [TestCase("Ammunition", true, false, false, false)]
    [TestCase("Ammunition", false, false, false, false)]
    [TestCase("Ammunition", true, true, false, false)]
    [TestCase("Ammunition", false, true, false, false)]
    [TestCase("Energy", false, true, false, true)]
    [TestCase("Energy", true, true, false, false)]
    [TestCase("Energy", false, false, false, false)]
    [TestCase("Energy", true, false, false, false)]
    [TestCase("Energy", false, true, true, false)]
    [TestCase("Energy", true, true, true, false)]
    [TestCase("Energy", false, false, true, false)]
    [TestCase("Energy", true, false, true, false)]
    [TestCase("Hybrid", true, true, true, false)]
    public void SupplyAllowsExactlyTheSelectedShape(string mode, bool ammo, bool energy, bool reload, bool valid)
    {
        foreach (var category in new[] { "light_weapon", "medium_weapon", "heavy_weapon" })
        {
            var document = WeaponDocument(category);
            var supply = new JObject { ["Mode"] = mode };
            if (ammo) supply["AmmoCapacity"] = 60;
            if (energy) supply["EnergyPerShot"] = 4;
            if (reload) supply["ReloadDuration"] = 1500;
            document["Items"]![0]!["Supply"] = supply;
            AssertSchemaAndParser(document, category, valid);
        }
    }

    [TestCase("missing")]
    [TestCase("null")]
    [TestCase("scalar")]
    [TestCase("mode_missing")]
    [TestCase("mode_null")]
    [TestCase("mode_unknown")]
    [TestCase("capacity_null")]
    [TestCase("capacity_fractional")]
    [TestCase("capacity_zero")]
    [TestCase("capacity_negative")]
    [TestCase("capacity_string")]
    [TestCase("reload_zero")]
    [TestCase("reload_negative")]
    [TestCase("reload_null")]
    [TestCase("energy_zero")]
    [TestCase("energy_negative")]
    [TestCase("energy_null")]
    [TestCase("foreign_zero")]
    [TestCase("foreign_null")]
    [TestCase("unknown_field")]
    public void InvalidSupplyCannotEnterDefinitions(string mutation)
    {
        var document = WeaponDocument();
        var row = (JObject)document["Items"]![0]!;
        var supply = (JObject)row["Supply"]!;
        switch (mutation)
        {
            case "missing": row.Remove("Supply"); break;
            case "null": row["Supply"] = null; break;
            case "scalar": row["Supply"] = "Ammunition"; break;
            case "mode_missing": supply.Remove("Mode"); break;
            case "mode_null": supply["Mode"] = null; break;
            case "mode_unknown": supply["Mode"] = "ammunition"; break;
            case "capacity_null": supply["AmmoCapacity"] = null; break;
            case "capacity_fractional": supply["AmmoCapacity"] = 2.5; break;
            case "capacity_zero": supply["AmmoCapacity"] = 0; break;
            case "capacity_negative": supply["AmmoCapacity"] = -1; break;
            case "capacity_string": supply["AmmoCapacity"] = "60"; break;
            case "reload_zero": supply["ReloadDuration"] = 0; break;
            case "reload_negative": supply["ReloadDuration"] = -1; break;
            case "reload_null": supply["ReloadDuration"] = null; break;
            case "energy_zero": row["Supply"] = new JObject { ["Mode"] = "Energy", ["EnergyPerShot"] = 0 }; break;
            case "energy_negative": row["Supply"] = new JObject { ["Mode"] = "Energy", ["EnergyPerShot"] = -1 }; break;
            case "energy_null": row["Supply"] = new JObject { ["Mode"] = "Energy", ["EnergyPerShot"] = null }; break;
            case "foreign_zero": supply["EnergyPerShot"] = 0; break;
            case "foreign_null": supply["EnergyPerShot"] = null; break;
            case "unknown_field": supply["Cost"] = 1; break;
        }
        AssertSchemaAndParser(document, "light_weapon", false);
    }

    [TestCase("HitScan", "Ammunition")]
    [TestCase("HitScan", "Energy")]
    [TestCase("Projectile", "Ammunition")]
    [TestCase("Projectile", "Energy")]
    public void DeliveryAndSupplyAreIndependent(string delivery, string mode)
    {
        var document = WeaponDocument();
        var row = document["Items"]![0]!;
        row["DeliveryType"] = delivery;
        if (mode == "Energy") row["Supply"] = new JObject { ["Mode"] = mode, ["EnergyPerShot"] = 4 };
        AssertSchemaAndParser(document, "light_weapon", true);
        var definition = (WeaponItemDefinition)Parse(document)["vulcan_mk1"];
        Assert.That(definition.DeliveryType.ToString(), Is.EqualTo(delivery));
        Assert.That(definition.Supply.GetType(), Is.EqualTo(mode == "Energy" ? typeof(EnergySupplyDefinition) : typeof(AmmunitionSupplyDefinition)));
    }

    [TestCase("Archetype")]
    [TestCase("DeliveryType")]
    [TestCase("PresentationId")]
    [TestCase("DamageSource")]
    [TestCase("Damage")]
    [TestCase("AttackSpeed")]
    [TestCase("Range")]
    [TestCase("EnergyConsumption")]
    public void RequiredWeaponFieldsCannotBeOmitted(string field)
    {
        var document = WeaponDocument();
        ((JObject)document["Items"]![0]!).Remove(field);
        AssertSchemaAndParser(document, "light_weapon", false);
    }

    [TestCase("engine")]
    [TestCase("hull")]
    [TestCase("shield_generator")]
    [TestCase("reactor")]
    [TestCase("battery")]
    [TestCase("weapon_fixture")]
    [TestCase("light_weapon")]
    [TestCase("medium_weapon")]
    [TestCase("heavy_weapon")]
    [TestCase("consumable")]
    [TestCase("resource")]
    public void EveryCategoryRequiresExplicitNonnegativeMass(string category)
    {
        foreach (var mutation in new[] { "missing", "null", "negative", "string" })
        {
            var document = GameplayTestData.Read($"items/{category}.yml", $"items/{category}.schema.yml");
            var row = (JObject)document["Items"]![0]!;
            if (mutation == "missing") row.Remove("Mass");
            else if (mutation == "null") row["Mass"] = null;
            else if (mutation == "negative") row["Mass"] = -1;
            else row["Mass"] = "1";
            AssertSchemaAndParser(document, category, false);
        }
    }

    [TestCase("Values")]
    [TestCase("Weapon")]
    [TestCase("SupplyMode")]
    [TestCase("AmmoCapacity")]
    [TestCase("EnergyPerShot")]
    [TestCase("MagazinSize")]
    [TestCase("MagazineCapacity")]
    public void LegacyAndMisplacedFieldsAreRejectedEvenWithCurrentVersion(string field)
    {
        var document = WeaponDocument();
        document["Items"]![0]![field] = field is "Values" or "Weapon" ? new JObject() : new JValue(1);
        AssertSchemaAndParser(document, "light_weapon", false);
    }

    [Test]
    public void CategoryValueListsAndOldDocumentVersionsAreRejected()
    {
        var categories = GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml");
        categories["Categories"]![0]!["Values"] = "Mass,Speed";
        AssertSchema(categories, "items/ItemCategorySchema.yml", false);
        Assert.Catch<ArgumentException>(() => ItemTestData.ParseCategories(categories, _ => null!));
        var document = WeaponDocument();
        document["Version"] = "2.0.0";
        AssertSchemaAndParser(document, "light_weapon", false);
    }

    [Test]
    public void OmittedSpreadDefaultsToZeroAndExplicitSpreadIsValidated()
    {
        var document = WeaponDocument();
        var row = (JObject)document["Items"]![0]!;
        row.Remove("SpreadDegrees");
        AssertSchemaAndParser(document, "light_weapon", true);
        Assert.That(((WeaponItemDefinition)Parse(document)["vulcan_mk1"]).SpreadDegrees, Is.Zero);
        foreach (var value in new[] { -1d, 181d })
        {
            row["SpreadDegrees"] = value;
            AssertSchemaAndParser(document, "light_weapon", false);
        }
        row["SpreadDegrees"] = null;
        AssertSchemaAndParser(document, "light_weapon", false);
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void ParserRejectsNonfiniteNumbers(double value)
    {
        foreach (var field in new[] { "Mass", "Damage", "AttackSpeed", "Range", "SpreadDegrees", "EnergyConsumption" })
        {
            var document = WeaponDocument();
            document["Items"]![0]![field] = value;
            Assert.Catch<ArgumentException>(() => Parse(document), field);
        }
        foreach (var mode in new[] { "Ammunition", "Energy" })
        {
            var document = WeaponDocument();
            document["Items"]![0]!["Supply"] = mode == "Energy"
                ? new JObject { ["Mode"] = mode, ["EnergyPerShot"] = value }
                : new JObject { ["Mode"] = mode, ["AmmoCapacity"] = 60, ["ReloadDuration"] = value };
            Assert.Catch<ArgumentException>(() => Parse(document), mode);
        }
    }

    private static JObject WeaponDocument(string category = "light_weapon")
    {
        var document = GameplayTestData.Read("items/light_weapon.yml", "items/light_weapon.schema.yml");
        var weapon = GameplayTestData.Find((JArray)document["Items"]!, "vulcan_mk1");
        weapon["CategoryId"] = category;
        document["Items"] = new JArray(weapon);
        return document;
    }

    private static Dictionary<string, ItemDefinition> Parse(JObject document)
    {
        var categories = ItemTestData.ParseCategories(
            GameplayTestData.Read("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml"), _ => null!);
        return ItemTestData.ParseItems(document, categories, _ => null!);
    }

    private static void AssertSchemaAndParser(JObject document, string category, bool valid)
    {
        AssertSchema(document, $"items/{category}.schema.yml", valid);
        AssertSchema(document, "items/ItemSchema.yml", valid);
        if (valid) Assert.DoesNotThrow(() => Parse(document), document.ToString());
        else Assert.Catch<ArgumentException>(() => Parse(document), document.ToString());
    }

    private static void AssertSchema(JObject document, string schema, bool valid)
    {
        var result = YamlParser.ValidateAndReturn(document.ToString(),
            File.ReadAllText(Path.Combine(GameplayTestData.DirectoryPath, schema)), "test", schema);
        Assert.That(result.IsSuccess, Is.EqualTo(valid), $"{schema}: {document}\n{result.Error?.ToDisplayString()}");
    }

    private static void AssertNoDictionaryProperties(Type type)
    {
        Assert.That(type.GetProperty("Values"), Is.Null);
        foreach (var property in type.GetProperties())
        {
            var propertyType = property.PropertyType;
            Assert.That(typeof(IDictionary).IsAssignableFrom(propertyType), Is.False, $"{type.Name}.{property.Name}");
            Assert.That(typeof(JToken).IsAssignableFrom(propertyType), Is.False, $"{type.Name}.{property.Name}");
            if (!propertyType.IsGenericType) continue;
            var definition = propertyType.GetGenericTypeDefinition();
            Assert.That(definition, Is.Not.EqualTo(typeof(IReadOnlyDictionary<,>)).And.Not.EqualTo(typeof(IDictionary<,>)), $"{type.Name}.{property.Name}");
        }
    }
}
