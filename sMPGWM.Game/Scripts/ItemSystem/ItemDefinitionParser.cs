using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public static class ItemDefinitionParser
{
    private static readonly CatalogueSource ItemCategorySource =
        new("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml", "Categories");
    private static readonly CatalogueSource ItemSource =
        new("ItemDefinitions.yml", "items/ItemSchema.yml", "Items");
    private static readonly string[] CommonFields =
        ["Id", "DisplayName", "CategoryId", "Description", "Icon", "CanStack", "StackLimit", "Mass", "AbilityIds", "BehaviourGrants"];
    private static readonly string[] CategoryFields = ["Id", "DisplayName", "Description", "Icon", "CanStack", "StackLimit"];
    private static readonly string[] EngineFields = ["Speed", "AfterBurnerSpeed", "RotationSpeed", "Acceleration", "AfterBurnerAcceleration", "EnergyConsumption"];
    private static readonly string[] HullFields = ["Armor"];
    private static readonly string[] ShieldFields = ["ShieldCapacity", "RechargeTime", "DownTime", "EnergyConsumption"];
    private static readonly string[] ReactorFields = ["EnergyProduction"];
    private static readonly string[] BatteryFields = ["EnergyStorage", "EnergyConsumption"];
    private static readonly string[] FixtureFields = ["WeaponSlots"];
    private static readonly string[] WeaponFields = ["Archetype", "DeliveryType", "PresentationId", "DamageSource", "Damage", "AttackSpeed", "Range", "SpreadDegrees", "EnergyConsumption", "Supply"];
    private static readonly string[] AmmunitionFields = ["Mode", "AmmoCapacity", "ReloadDuration"];
    private static readonly string[] EnergyFields = ["Mode", "EnergyPerShot"];
    private static readonly string[] GrantFields = ["BehaviourId", "While", "Lifetime"];

    public static Dictionary<string, ItemCategoryDefinition> LoadItemCategoryDefinitions(
        JObject yaml, Func<string, IconDefinition> resolveIcon)
    {
        ValidateDocument(yaml, "Categories", ItemCategorySource.FileName);
        var definitions = new Dictionary<string, ItemCategoryDefinition>(StringComparer.Ordinal);
        foreach (var definition in CatalogueParser.Parse(yaml, ItemCategorySource, row =>
        {
            CheckFields((JObject)row, CategoryFields);
            var id = RequiredString(row, "Id");
            var (canStack, limit) = ResolveStacking(row, false, 1);
            return new ItemCategoryDefinition(id, RequiredString(row, "DisplayName"),
                resolveIcon(row["Icon"] is null ? "Software.WarningSignCircleQuestionMarkHelp" : IconKey(row)))
            {
                Description = RequiredString(row, "Description"),
                CanStack = canStack,
                StackLimit = limit
            };
        })) definitions.Add(definition.Id, definition);
        return definitions;
    }

    public static Dictionary<string, ItemDefinition> LoadItemDefinitions(JObject yaml,
        Dictionary<string, ItemCategoryDefinition> categories, Func<string, IconDefinition> resolveIcon)
    {
        ValidateDocument(yaml, "Items", ItemSource.FileName);
        var definitions = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        foreach (var definition in CatalogueParser.Parse(yaml, ItemSource, row =>
        {
            var categoryId = RequiredString(row, "CategoryId");
            if (!categories.TryGetValue(categoryId, out var category))
                throw new ArgumentException($"CategoryId: unknown category '{categoryId}'.");
            CheckFields((JObject)row, CommonFields, FieldsFor(categoryId));
            var (canStack, limit) = ResolveStacking(row, category.CanStack, category.StackLimit);
            var id = RequiredString(row, "Id");
            var name = RequiredString(row, "DisplayName");
            var description = RequiredString(row, "Description");
            var icon = row["Icon"] is null || row["Icon"]!.Type == JTokenType.Null
                ? category.Icon!
                : resolveIcon(IconKey(row));
            var mass = BoundedNumber(row, "Mass", float.MaxValue / 2);
            var abilities = ReadAbilities(row, categoryId == "consumable");
            var grants = ReadGrants(row);
            ItemDefinition item = categoryId switch
            {
                "engine" => new EngineItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon)
                {
                    Mass = mass, Speed = BoundedNumber(row, "Speed", float.MaxValue), AfterBurnerSpeed = BoundedNumber(row, "AfterBurnerSpeed", float.MaxValue),
                    RotationSpeed = BoundedNumber(row, "RotationSpeed", float.MaxValue), Acceleration = BoundedNumber(row, "Acceleration", float.MaxValue),
                    AfterBurnerAcceleration = BoundedNumber(row, "AfterBurnerAcceleration", float.MaxValue), EnergyConsumption = Number(row, "EnergyConsumption")
                },
                "hull" => new HullItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon)
                {
                    Mass = mass, Armor = Number(row, "Armor")
                },
                "shield_generator" => new ShieldGeneratorItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon)
                {
                    Mass = mass, ShieldCapacity = Number(row, "ShieldCapacity"), RechargeTime = Number(row, "RechargeTime", positive: true),
                    DownTime = BoundedNumber(row, "DownTime", long.MaxValue / 60d), EnergyConsumption = Number(row, "EnergyConsumption")
                },
                "reactor" => new ReactorItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon)
                {
                    Mass = mass, EnergyProduction = Number(row, "EnergyProduction")
                },
                "battery" => new BatteryItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon)
                {
                    Mass = mass, EnergyStorage = Number(row, "EnergyStorage"), EnergyConsumption = Number(row, "EnergyConsumption")
                },
                "weapon_fixture" => new WeaponFixtureItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon)
                {
                    Mass = mass, WeaponSlots = Integer(row, "WeaponSlots", 0, 9)
                },
                "light_weapon" or "medium_weapon" or "heavy_weapon" => new WeaponItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon)
                {
                    Mass = mass, Archetype = RequiredString(row, "Archetype"), DeliveryType = ReadDelivery(row),
                    PresentationId = RequiredString(row, "PresentationId"), DamageSource = RequiredString(row, "DamageSource"),
                    Damage = Number(row, "Damage"), AttackSpeed = Number(row, "AttackSpeed", positive: true),
                    Range = Number(row, "Range", positive: true), SpreadDegrees = ReadSpread(row),
                    EnergyConsumption = Number(row, "EnergyConsumption"), Supply = ReadSupply(row)
                },
                "consumable" => new ConsumableItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon) { Mass = mass },
                "resource" => new ResourceItemDefinition(id, name, category.DisplayName, description, canStack, limit, icon) { Mass = mass },
                _ => throw new ArgumentException($"CategoryId: unsupported category '{categoryId}'.")
            };
            return item with { CategoryId = category.Id, AbilityIds = abilities, BehaviourGrants = grants };
        })) definitions.Add(definition.Id, definition);
        return definitions;
    }

    private static void ValidateDocument(JObject document, string root, string source)
    {
        foreach (var property in document.Properties())
            if (property.Name != "Version" && property.Name != root)
                throw new ArgumentException($"{source}: unexpected field '{property.Name}'.");
        if (document["Version"]?.Type != JTokenType.String || (string?)document["Version"] != "3.0.0")
            throw new ArgumentException($"{source}: Version must be 3.0.0; migrate legacy item data to flat properties and typed Supply.");
        if (document[root] is not JArray rows)
            throw new ArgumentException($"{source}: {root}: expected an array.");
        if (root == "Categories" && rows.Count == 0)
            throw new ArgumentException($"{source}: Categories: expected at least one category.");
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i] is not JObject)
                throw new ArgumentException($"{source}: {root}[{i}]: expected an object.");
            if (rows[i]["Id"]?.Type != JTokenType.String)
                throw new ArgumentException($"{source}: {root}[{i}].Id: required nonempty string.");
        }
    }

    private static string[] FieldsFor(string categoryId) => categoryId switch
    {
        "engine" => EngineFields,
        "hull" => HullFields,
        "shield_generator" => ShieldFields,
        "reactor" => ReactorFields,
        "battery" => BatteryFields,
        "weapon_fixture" => FixtureFields,
        "light_weapon" or "medium_weapon" or "heavy_weapon" => WeaponFields,
        "consumable" or "resource" => Array.Empty<string>(),
        _ => throw new ArgumentException($"CategoryId: unsupported category '{categoryId}'.")
    };

    private static void CheckFields(JObject row, string[] allowed, string[]? additional = null, string prefix = "")
    {
        foreach (var property in row.Properties())
        {
            if (Array.IndexOf(allowed, property.Name) >= 0 || additional != null && Array.IndexOf(additional, property.Name) >= 0) continue;
            throw new ArgumentException($"{prefix}{property.Name}: unexpected field; migrate legacy data to the current item schema.");
        }
    }

    private static string RequiredString(JToken row, string field)
    {
        if (row[field]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)row[field]))
            throw new ArgumentException($"{field}: required nonempty string.");
        return (string)row[field]!;
    }

    private static string IconKey(JToken row)
    {
        var key = RequiredString(row, "Icon");
        if (!Regex.IsMatch(key, @"\A[A-Za-z][A-Za-z0-9_]*\.[A-Za-z][A-Za-z0-9_]*\z"))
            throw new ArgumentException("Icon: expected a Group.Name icon key.");
        return key;
    }

    private static double BoundedNumber(JToken row, string field, double maximum)
    {
        var value = Number(row, field);
        if (value > maximum) throw new ArgumentException($"{field}: exceeds the runtime maximum {maximum}.");
        return value;
    }

    private static double Number(JToken row, string field, bool positive = false)
    {
        if (row[field]?.Type is not (JTokenType.Integer or JTokenType.Float))
            throw new ArgumentException($"{field}: invalid value; expected a finite number.");
        var value = (double)row[field]!;
        if (!double.IsFinite(value) || (positive ? value <= 0 : value < 0))
            throw new ArgumentException($"{field}: invalid value; expected a finite {(positive ? "positive" : "nonnegative")} number.");
        return value;
    }

    private static int Integer(JToken row, string field, int minimum, int maximum)
    {
        var value = Number(row, field);
        if (value != Math.Truncate(value) || value < minimum || value > maximum)
            throw new ArgumentException($"{field}: invalid value; expected an integer between {minimum} and {maximum}.");
        return (int)value;
    }

    private static (bool CanStack, int StackLimit) ResolveStacking(JToken row, bool defaultCanStack, int defaultLimit)
    {
        if (row["CanStack"] != null && row["StackLimit"] != null)
            throw new ArgumentException("CanStack/StackLimit: specify only one stacking setting.");
        var canStack = defaultCanStack;
        var limit = defaultLimit;
        if (row["CanStack"] is { } toggle)
        {
            if (toggle.Type != JTokenType.Boolean) throw new ArgumentException("CanStack: expected a boolean.");
            canStack = (bool)toggle;
            limit = canStack ? int.MaxValue : 1;
        }
        else if (row["StackLimit"] != null)
        {
            canStack = true;
            limit = Integer(row, "StackLimit", 0, int.MaxValue - 1);
        }
        if (canStack ? limit < 2 : limit != 1)
            throw new ArgumentException("CanStack/StackLimit: resolved stacking configuration is inconsistent.");
        return (canStack, limit);
    }

    private static WeaponDeliveryType ReadDelivery(JToken row) => RequiredString(row, "DeliveryType") switch
    {
        "HitScan" => WeaponDeliveryType.HitScan,
        "Projectile" => WeaponDeliveryType.Projectile,
        var delivery => throw new ArgumentException($"DeliveryType: unsupported value '{delivery}'.")
    };

    private static double ReadSpread(JToken row)
    {
        if (row["SpreadDegrees"] is null) return 0;
        var spread = Number(row, "SpreadDegrees");
        if (spread > 180) throw new ArgumentException("SpreadDegrees: invalid value; expected 0..180.");
        return spread;
    }

    private static WeaponSupplyDefinition ReadSupply(JToken row)
    {
        if (row["Supply"] is not JObject supply) throw new ArgumentException("Supply: required object.");
        try
        {
            switch (RequiredString(supply, "Mode"))
            {
                case "Ammunition":
                    CheckFields(supply, AmmunitionFields);
                    return new AmmunitionSupplyDefinition(Integer(supply, "AmmoCapacity", 1, int.MaxValue),
                        Number(supply, "ReloadDuration", positive: true));
                case "Energy":
                    CheckFields(supply, EnergyFields);
                    return new EnergySupplyDefinition(Number(supply, "EnergyPerShot", positive: true));
                default:
                    throw new ArgumentException("Mode: expected Ammunition or Energy.");
            }
        }
        catch (ArgumentException error)
        {
            throw new ArgumentException($"Supply.{error.Message}", error);
        }
    }

    private static IReadOnlyList<string> ReadAbilities(JToken row, bool required)
    {
        if (row["AbilityIds"] is null && !required) return Array.Empty<string>();
        if (row["AbilityIds"] is not JArray ids || ids.Count == 0)
            throw new ArgumentException("AbilityIds: expected a nonempty array.");
        var abilities = new string[ids.Count];
        for (var i = 0; i < ids.Count; i++) abilities[i] = Identifier(ids[i], $"AbilityIds[{i}]");
        return Array.AsReadOnly(abilities);
    }

    private static IReadOnlyList<ItemBehaviourGrant> ReadGrants(JToken row)
    {
        if (row["BehaviourGrants"] is null) return Array.Empty<ItemBehaviourGrant>();
        if (row["BehaviourGrants"] is not JArray grants || grants.Count == 0)
            throw new ArgumentException("BehaviourGrants: expected a nonempty array.");
        var result = new ItemBehaviourGrant[grants.Count];
        for (var i = 0; i < grants.Count; i++)
        {
            if (grants[i] is not JObject grant) throw new ArgumentException($"BehaviourGrants[{i}]: expected an object.");
            CheckFields(grant, GrantFields, prefix: $"BehaviourGrants[{i}].");
            var id = Identifier(grant["BehaviourId"], $"BehaviourGrants[{i}].BehaviourId");
            var whileMode = RequiredString(grant, "While") switch
            {
                "Carried" => BehaviourGrantWhile.Carried,
                "Equipped" => BehaviourGrantWhile.Equipped,
                _ => throw new ArgumentException($"BehaviourGrants[{i}].While: expected Carried or Equipped.")
            };
            if (RequiredString(grant, "Lifetime") != "WhileSourceActive")
                throw new ArgumentException($"BehaviourGrants[{i}].Lifetime: expected WhileSourceActive.");
            result[i] = new ItemBehaviourGrant(id, whileMode, BehaviourGrantLifetime.WhileSourceActive);
        }
        return Array.AsReadOnly(result);
    }

    private static string Identifier(JToken? token, string field)
    {
        if (token?.Type != JTokenType.String || string.IsNullOrEmpty((string?)token))
            throw new ArgumentException($"{field}: required identifier.");
        var id = (string)token!;
        if (id.Length > 64) throw new ArgumentException($"{field}: identifier exceeds 64 characters.");
        if (id[0] is < 'a' or > 'z') throw new ArgumentException($"{field}: invalid identifier '{id}'.");
        for (var i = 1; i < id.Length; i++)
            if (id[i] is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_'))
                throw new ArgumentException($"{field}: invalid identifier '{id}'.");
        return id;
    }
}
