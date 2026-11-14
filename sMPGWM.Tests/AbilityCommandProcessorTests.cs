using sMPGWM.Scripts.AbilitySystem;

namespace sMPGWM.Tests;

public sealed class AbilityCommandProcessorTests
{
    [Test]
    public void QueueIsBoundedAndClearRestoresCapacity()
    {
        var processor = new AbilityCommandProcessor();
        var command = (1, 1L, "caster", 1L, "target", "ability", AbilitySource.Equipment("slot", 1));
        for (var i = 0; i < 32; i++) processor.Enqueue(command);
        Assert.That(processor.Count, Is.EqualTo(32));
        Assert.Throws<InvalidOperationException>(() => processor.Enqueue(command));
        processor.Clear();
        processor.Enqueue(command);
        Assert.That(processor.Count, Is.EqualTo(1));
    }
}
