using sMPGWM.Scripts.Controllers.LivingEntity;
using sMPGWM.Scripts.Controllers.LivingEntity.Modifiers;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Tests;

[TestFixture]
public class StatTests
{
    [Test]
    public void BaseValue_WithPermanentModifiers_CalculatesMaxAndCurrentCorrectly()
    {
        var stat = new Stat(100);
        var modifier = new StatModifier(-90, ModifierType.Flat);

        var modifierReducePercentage = new StatModifier(-0.8f, ModifierType.Percent);
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 100, equals baseValue. no modifier applied
            // currentValue = 100, equals maxValue. no changes applied
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.CurrentValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(100f));
        });

        stat.AddModifier(new StatModifier(20, ModifierType.Flat));
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 120, former = 100, maxValue gets raised by 20
            // currentValue = 120, former = 100, currentValue gets raised with the same amount as maxValue
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.CurrentValue, Is.EqualTo(120f));
            Assert.That(stat.MaxValue, Is.EqualTo(120f));
        });

        stat.AddModifier(new StatModifier(0.5f, ModifierType.Percent));
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 180, former = 120, (100 + 20) * 1.5 = 180 maxValue. first flat modifiers are applied, then percentage
            // currentValue = 180, former = 120, currentValue gets raised with the same amount as maxValue
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.CurrentValue, Is.EqualTo(180f));
            Assert.That(stat.MaxValue, Is.EqualTo(180f));
        });

        stat.AddModifier(modifier);
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 45, former = 180, (100 + 20 - 90) * 1.5 = 45.0  maxValue, first flat modifiers are applied, then percentage
            // currentValue = 45, former = 180, currentValue gets reduced with the same amount as maxValue
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.CurrentValue, Is.EqualTo(45f));
            Assert.That(stat.MaxValue, Is.EqualTo(45f));
        });

        stat.AddModifier(new StatModifier(0.25f, ModifierType.Percent));
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 52.5, former = 45, (100 + 20 - 90) * (1.5 + 1.25) = 52.5 maxValue, first flat modifiers are applied, then percentage
            // currentValue = 52.5, former = 45, 45 + (52.5 - 45) = 52.5 currentValue gets raised with the same amount as maxValue but raised by 7.5, not set!
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(52.5f));
            Assert.That(stat.CurrentValue, Is.EqualTo(52.5f));
        });

        stat.Reduce(25f);
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 52.5, former = 45, (100 + 20 - 90) * (1.5 + 1.25) = 52.5 maxValue, first flat modifiers are applied, then percentage
            // currentValue = 27.5, former = 52.5, 52.5 - 25 = 27.5 currentValue gets reduced
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(52.5f));
            Assert.That(stat.CurrentValue, Is.EqualTo(27.5f));
        });

        stat.Raise(100f);
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 52.5, former = 45, (100 + 20 - 90) * (1.5 + 1.25) = 52.5 maxValue, first flat modifiers are applied, then percentage
            // currentValue = 52.5, former 27.5,  currentValue gets raised by 100 from heal amount, but cannot exceed maxValue
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(52.5f));
            Assert.That(stat.CurrentValue, Is.EqualTo(52.5f));
        });

        stat.Reduce(5f);
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 52.5, former = 52.5, (100 + 20 - 90) * (1.5 + 1.25) = 52.5 maxValue, first flat modifiers are applied, then percentage
            // currentValue = 47.5, former 52.5,  currentValue gets reduced by 5
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(52.5f));
            Assert.That(stat.CurrentValue, Is.EqualTo(47.5f));
        });

        stat.RemoveModifier(modifier);
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 210, former = 52.5, (100 + 20) * (1.5 + 1.25) = 210 maxValue, first flat modifiers are applied, then percentage
            // currentValue = 205, former 52.5,  47.5 + (210 - 52.5) = 205 currentValue gets raised with the same amount as maxValue but raised by 157.5, not set!
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(210f));
            Assert.That(stat.CurrentValue, Is.EqualTo(205f));
        });

        stat.AddModifier(modifierReducePercentage);
        Assert.Multiple(() =>
        {
            // baseValue = 100
            // maxValue = 210, former = 52.5, (100 + 20) * (1.5 + 1.25 - 1.8) = 114 maxValue, first flat modifiers are applied, then percentage
            // currentValue = 205, former 205,  205 + (114 - 210) = 109 currentValue gets reduced with the same amount as maxValue but reduced by 96, not set!
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(114f));
            Assert.That(stat.CurrentValue, Is.EqualTo(109f));
        });
    }

    [Test]
    public void TemporaryModifier_Expires_AfterTick_CalculatesMaxAndCurrentCorrecty()
    {
        var stat = new Stat(100);

        // Add permanent flat modifier (+20)
        stat.AddModifier(new StatModifier(20, ModifierType.Flat));

        // Add permanent % modifier (+50%)
        stat.AddModifier(new StatModifier(0.5f, ModifierType.Percent));

        Assert.Multiple(() =>
        {
            // (100 + 20) * 1.5 = 180
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.PermanentMaxValue, Is.EqualTo(180f));
            Assert.That(stat.MaxValue, Is.EqualTo(180f));
            Assert.That(stat.CurrentValue, Is.EqualTo(180f));
        });

        // Apply temporary modfier: -30 flat for 2 seconds
        stat.AddModifier(new TemporaryStatModifier(-30f, ModifierType.Flat, 2f));

        Assert.Multiple(() =>
        {
            // (180 - 30) = 150 (flat-only temp modifier)
            Assert.That(stat.MaxValue, Is.EqualTo(150f));
            Assert.That(stat.CurrentValue, Is.EqualTo(150f));
        });

        // Tick 1s (should still be active)
        stat.Tick(1f);
        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(150f));
            Assert.That(stat.CurrentValue, Is.EqualTo(150f));
        });

        // Tick another 1.1s (modifier should expire)
        stat.Tick(1.1f);
        Assert.Multiple(() =>
        {
            // Modifier is gone, max should return to 180
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.MaxValue, Is.EqualTo(180f));
            Assert.That(stat.CurrentValue, Is.EqualTo(180f));
        });
    }

    [Test]
    public void PermanentAndTemporaryModifiers_InteractionOverTime()
    {
        var stat = new Stat(100);

        // Add permanent flat +10
        stat.AddModifier(new StatModifier(10, ModifierType.Flat));

        // Add permanent % +0.5
        stat.AddModifier(new StatModifier(0.5f, ModifierType.Percent));

        Assert.Multiple(() =>
        {
            // (100 + 10) * 1.5 = 165
            Assert.That(stat.BaseValue, Is.EqualTo(100f));
            Assert.That(stat.PermanentMaxValue, Is.EqualTo(165f));
            Assert.That(stat.MaxValue, Is.EqualTo(165f));
            Assert.That(stat.CurrentValue, Is.EqualTo(165f));
        });

        // Apply temporary flat +20
        stat.AddModifier(new TemporaryStatModifier(20f, ModifierType.Flat, 3f));

        Assert.Multiple(() =>
        {
            // (165 + 20) = 185 (flat bonus added)
            Assert.That(stat.MaxValue, Is.EqualTo(185f));
            Assert.That(stat.CurrentValue, Is.EqualTo(185f));
        });

        // Apply temporary % +0.2
        stat.AddModifier(new TemporaryStatModifier(0.2f, ModifierType.Percent, 2f));

        Assert.Multiple(() =>
        {
            // (165 + 20) * 1.2 = 222
            Assert.That(stat.MaxValue, Is.EqualTo(222f));
            Assert.That(stat.CurrentValue, Is.EqualTo(222f));
        });

        // Reduce by 50
        stat.Reduce(50);
        Assert.Multiple(() => { Assert.That(stat.CurrentValue, Is.EqualTo(172f)); });

        // Tick 1s
        stat.Tick(1f);
        Assert.Multiple(() =>
        {
            // Still active: +20 flat (2s left), +0.2% (1s left)
            // Max should remain the same
            Assert.That(stat.MaxValue, Is.EqualTo(222f));
            Assert.That(stat.CurrentValue, Is.EqualTo(172f));
        });

        // Tick 1.1s: % modifier expires
        stat.Tick(1.1f);
        Assert.Multiple(() =>
        {
            // (165 + 20) * 1.0 = 185
            // CurrentValue = 172 + (185 - 222) = 135
            Assert.That(stat.MaxValue, Is.EqualTo(185f));
            Assert.That(stat.CurrentValue, Is.EqualTo(135f));
        });

        // Tick 1s more: flat modifier expires
        stat.Tick(1f);
        Assert.Multiple(() =>
        {
            // Back to (100 + 10) * 1.5 = 165
            // Current = 135 + (165 - 185) = 115
            Assert.That(stat.PermanentMaxValue, Is.EqualTo(165f));
            Assert.That(stat.MaxValue, Is.EqualTo(165f));
            Assert.That(stat.CurrentValue, Is.EqualTo(115f));
        });

        // Raise 100
        stat.Raise(100f);
        Assert.Multiple(() =>
        {
            // CurrentValue should be clamped to max
            Assert.That(stat.CurrentValue, Is.EqualTo(165f));
        });
    }


    [Test]
    public void TemporaryFlatModifier_LowersMaxAndAdjustsCurrentByDelta()
    {
        var stat = new Stat(100);
        stat.Reduce(10); // Current = 90

        stat.AddModifier(new TemporaryStatModifier(-10, ModifierType.Flat, 5f));

        Assert.Multiple(() =>
        {
            Assert.That(stat.MaxValue, Is.EqualTo(90f));
            Assert.That(stat.CurrentValue, Is.EqualTo(80f)); // 90 - 10 delta
        });
    }


    [Test]
    public void TemporaryModifier_Expires_AfterTick()
    {
        var stat = new Stat(100);

        var modifier = new TemporaryStatModifier(-20, ModifierType.Flat, 1f);

        stat.AddModifier(modifier);

        Assert.That(stat.MaxValue, Is.EqualTo(80f));

        stat.Tick(1.1f); // Modifier expires

        Assert.That(stat.MaxValue, Is.EqualTo(100f));
    }

    [Test]
    public void Raising_RespectsMaxValue()
    {
        var stat = new Stat(100);
        stat.Reduce(50);
        stat.Raise(30); // 50 - 30 = 80

        Assert.That(stat.CurrentValue, Is.EqualTo(80f));

        stat.Raise(50); // Should clamp to max
        Assert.That(stat.CurrentValue, Is.EqualTo(100f));
    }

    [Test]
    public void SetBaseValue_UpdatesMaxAndClampsCurrent()
    {
        var stat = new Stat(100);
        stat.Reduce(10); // Current = 90

        stat.SetBaseValue(50);
        Assert.Multiple(() =>
        {
            // Max = 50, Current = clamped to 50
            Assert.That(stat.MaxValue, Is.EqualTo(50f));
            Assert.That(stat.CurrentValue, Is.EqualTo(50f));
        });
    }

    [Test]
    public void RecurringModifier_Expires_AfterDuration()
    {
        var stat = new Stat(100);
        var recurring = new RecurringStatModifier(-10f, ModifierType.Flat, 4f, 2f, stat);

        stat.AddRecurringModifier(recurring);
        Assert.That(stat.CurrentValue, Is.EqualTo(100f));

        stat.Tick(2f); // First tick
        Assert.That(stat.CurrentValue, Is.EqualTo(90f));

        stat.Tick(2f); // Second tick
        Assert.That(stat.CurrentValue, Is.EqualTo(80f));

        stat.Tick(0.1f); // Should now expire
        stat.Tick(2f); // No more effect
        Assert.That(stat.CurrentValue, Is.EqualTo(80f));
    }
}
