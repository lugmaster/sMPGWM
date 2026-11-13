using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public static class ItemDefinitionParser
{
    private static readonly CatalogueSource ItemCategorySource =
        new("items/ItemCategoryDefinitions.yml", "items/ItemCategorySchema.yml", "Categories");
    private static readonly CatalogueSource ItemSource =
        new("ItemDefinitions.yml", "items/ItemSchema.yml", "Items");

    public static Dictionary<string, ItemCategoryDefinition> LoadItemCategoryDefinitions(
        JObject yaml, Func<string, IconDefinition> resolveIcon)
    {
        var definitions = new Dictionary<string, ItemCategoryDefinition>(StringComparer.Ordinal);
        foreach (var definition in CatalogueParser.Parse(yaml, ItemCategorySource, row =>
        {
            var (canStack, limit) = ResolveStacking(row, false, 1);
            var definition = new ItemCategoryDefinition((string)row["Id"]!, (string)row["DisplayName"]!,
                resolveIcon((string?)row["Icon"] ?? "Software.WarningSignCircleQuestionMarkHelp"))
            {
                Values = Array.AsReadOnly(((string?)row["Values"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                Description = (string?)row["Description"] ?? "",
                CanStack = canStack,
                StackLimit = limit
            };
            ValidateStacking(definition.CanStack, definition.StackLimit);
            return definition;
        })) definitions.Add(definition.Id, definition);
        return definitions;
    }

    private static (bool CanStack, int StackLimit) ResolveStacking(JToken row, bool defaultCanStack, int defaultLimit)
    {
        if (row["CanStack"] != null && row["StackLimit"] != null)
            throw new ArgumentException("CanStack/StackLimit: specify only one stacking setting.");
        if (row["CanStack"] is { } toggle)
            return ((bool)toggle, (bool)toggle ? int.MaxValue : 1);
        if (row["StackLimit"] is { } limit)
            return (true, (int)limit);
        return (defaultCanStack, defaultLimit);
    }

    private static void ValidateStacking(bool canStack, int limit)
    {
        if (canStack ? limit < 2 : limit != 1)
            throw new ArgumentException("CanStack/StackLimit: resolved stacking configuration is inconsistent.");
    }

    public static Dictionary<string, ItemDefinition> LoadItemDefinitions(JObject yaml,
        Dictionary<string, ItemCategoryDefinition> categories, Func<string, IconDefinition> resolveIcon)
    {
        var definitions = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        foreach (var definition in CatalogueParser.Parse(yaml, ItemSource, row =>
        {
            var categoryId = (string)row["CategoryId"]!;
            if (!categories.TryGetValue(categoryId, out var category))
                throw new ArgumentException($"CategoryId: unknown category '{categoryId}'.");
            var (canStack, limit) = ResolveStacking(row, category.CanStack, category.StackLimit);
            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            var abilities = new List<string>();
            var grants = new List<ItemBehaviourGrant>();
            if (row["AbilityIds"] is JArray ids)
                foreach (var id in ids) abilities.Add((string)id!);
            if (row["BehaviourGrants"] is JArray suppliedGrants)
                foreach (var grant in suppliedGrants)
                    grants.Add(new((string)grant["BehaviourId"]!,
                        Enum.Parse<BehaviourGrantWhile>((string)grant["While"]!),
                        Enum.Parse<BehaviourGrantLifetime>((string)grant["Lifetime"]!)));
            if (row["Values"] is JObject legacyValues)
            {
                if (legacyValues.ContainsKey("Shield"))
                    throw new ArgumentException($"Item '{row["Id"]}': replace legacy Shield with ShieldCapacity (item data version 2).");
                if (legacyValues.ContainsKey("Energy"))
                    throw new ArgumentException($"Item '{row["Id"]}': remove legacy Energy; use EnergyProduction on reactors and EnergyStorage on batteries.");
            }
            var weaponCategory = categoryId is "light_weapon" or "medium_weapon" or "heavy_weapon";
            WeaponDefinition? weapon = null;
            if (weaponCategory)
            {
                if (canStack) throw new ArgumentException("Weapons must be nonstackable.");
                weapon = ParseWeapon(row);
            }
            else if (row["Weapon"] != null) throw new ArgumentException("Weapon metadata requires a weapon category.");
            var requiredValues = new List<string>(category.Values);
            if (weapon != null)
            {
                if (weapon.SupplyMode == WeaponSupplyMode.Ammunition)
                {
                    requiredValues.Add("MagazineCapacity");
                    requiredValues.Add("ReloadDuration");
                }
                else requiredValues.Add("EnergyPerShot");
            }
            foreach (var required in requiredValues)
            {
                if (required == "AbilityIds")
                {
                    if (abilities.Count == 0) throw new ArgumentException($"Item '{row["Id"]}' requires AbilityIds.");
                    continue;
                }
                var value = row["Values"]?[required];
                if (value is not JValue scalar || scalar.Value == null ||
                    scalar.Type is not (JTokenType.Integer or JTokenType.Float or JTokenType.String))
                    throw new ArgumentException($"Item '{row["Id"]}' requires value '{required}'.");
                if ((required == "DamageSource") != (scalar.Type == JTokenType.String))
                    throw new ArgumentException($"Item '{row["Id"]}' has wrong type for '{required}'.");
                if (scalar.Type == JTokenType.String && (string.IsNullOrWhiteSpace((string?)scalar)))
                    throw new ArgumentException($"Item '{row["Id"]}' has invalid damage source.");
                if (scalar.Type != JTokenType.String && (!double.IsFinite((double)scalar) || (double)scalar < 0))
                    throw new ArgumentException($"Item '{row["Id"]}' has invalid value '{required}'.");
                if (required == "RechargeTime" && (double)scalar <= 0)
                    throw new ArgumentException($"Item '{row["Id"]}' requires positive RechargeTime in milliseconds.");
                values.Add(required, scalar.Value);
            }
            if (row["Values"] is JObject supplied && supplied.Count != values.Count)
                throw new ArgumentException($"Item '{row["Id"]}' contains unexpected values.");
            var definition = new ItemDefinition((string)row["Id"]!, (string)row["DisplayName"]!, category.DisplayName,
                (string?)row["Description"] ?? "", canStack, limit,
                row["Icon"]?.Type == JTokenType.String ? resolveIcon((string)row["Icon"]!) : category.Icon!)
            {
                CategoryId = category.Id,
                Weapon = weapon,
                Values = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(values),
                AbilityIds = abilities.AsReadOnly(),
                BehaviourGrants = grants.AsReadOnly()
            };
            ValidateStacking(definition.CanStack, definition.StackLimit);
            return definition;
        })) definitions.Add(definition.Id, definition);
        return definitions;
    }

    private static WeaponDefinition ParseWeapon(JToken row)
    {
        if (row["Values"]?["MagazinSize"] != null || row["Weapon"] is not JObject metadata)
            throw new ArgumentException("Weapon format 3.0.0 required: add Weapon: {Archetype: SMG, Delivery: HitScan, FireMode: Automatic, SupplyMode: Ammunition, PresentationId: smg_standard}; replace MagazinSize with MagazineCapacity, add Range and SpreadDegrees, and remove EnergyPerShot for ammunition weapons.");
        string Identifier(string key)
        {
            var value = (string?)metadata[key];
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
                throw new ArgumentException($"Weapon.{key}: expected identifier of at most 64 characters.");
            foreach (var character in value)
                if (!char.IsAsciiLetterOrDigit(character) && character != '_')
                    throw new ArgumentException($"Weapon.{key}: invalid identifier.");
            return value;
        }
        if (metadata.Count != 5) throw new ArgumentException("Weapon requires exactly Archetype, Delivery, FireMode, SupplyMode and PresentationId.");
        var archetype = Identifier("Archetype");
        if (Identifier("Delivery") != "HitScan" || Identifier("FireMode") != "Automatic")
            throw new ArgumentException("Only HitScan + Automatic weapons are currently supported.");
        var supplyName = Identifier("SupplyMode");
        var supply = supplyName switch
        {
            "Ammunition" => WeaponSupplyMode.Ammunition,
            "Energy" => WeaponSupplyMode.Energy,
            _ => throw new ArgumentException("Weapon.SupplyMode must be Ammunition or Energy.")
        };
        var profile = Identifier("PresentationId");
        if (profile is not ("smg_standard" or "laser_standard"))
            throw new ArgumentException($"Weapon.PresentationId: unknown profile '{profile}'.");
        double Number(string key)
        {
            if (row["Values"]?[key] is not JValue value || value.Type is not (JTokenType.Integer or JTokenType.Float) ||
                !double.IsFinite((double)value)) throw new ArgumentException($"Values.{key}: finite number required.");
            return (double)value;
        }
        var capacity = 0;
        var reloadTicks = 0L;
        var energy = 0d;
        if (supply == WeaponSupplyMode.Ammunition)
        {
            if (row["Values"]?["EnergyPerShot"] != null) throw new ArgumentException("Ammunition weapons forbid EnergyPerShot.");
            var authoredCapacity = Number("MagazineCapacity");
            var duration = Number("ReloadDuration");
            if (authoredCapacity < 1 || authoredCapacity > ushort.MaxValue || authoredCapacity != Math.Truncate(authoredCapacity))
                throw new ArgumentException("MagazineCapacity must be an integer in 1..65535.");
            if (duration <= 0 || duration > long.MaxValue / 120d * 1000d)
                throw new ArgumentException("ReloadDuration must be positive milliseconds with a representable tick deadline.");
            capacity = (int)authoredCapacity;
            reloadTicks = checked((long)Math.Max(1, Math.Ceiling(duration * 60 / 1000)));
        }
        else
        {
            if (row["Values"]?["MagazineCapacity"] != null || row["Values"]?["ReloadDuration"] != null)
                throw new ArgumentException("Energy weapons forbid MagazineCapacity and ReloadDuration.");
            energy = Number("EnergyPerShot");
            if (energy <= 0) throw new ArgumentException("EnergyPerShot must be positive.");
        }
        var stats = new ResolvedWeaponStats((string?)row["Values"]?["DamageSource"] ?? "", Number("Damage"),
            Number("AttackSpeed"), Number("Range"), Number("SpreadDegrees"), Number("EnergyConsumption"), energy, capacity, reloadTicks);
        if (!stats.IsValid) throw new ArgumentException("Invalid resolved weapon stats: AttackSpeed must be (0,60], Range positive and representable, SpreadDegrees 0..180, Damage and EnergyConsumption nonnegative.");
        return new(archetype, WeaponDelivery.HitScan, WeaponFireMode.Automatic, supply, profile, stats);
    }

}
