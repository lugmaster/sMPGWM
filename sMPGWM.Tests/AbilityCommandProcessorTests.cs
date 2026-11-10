using sMPGWM.Scripts.AbilitySystem;

namespace sMPGWM.Tests; // TEST-SUPPORT: COMMAND-BOUND-2 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity

public sealed class AbilityCommandProcessorTests // TEST-SUPPORT: COMMAND-BOUND-4 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
{
    [Test] // TEST-SUPPORT: COMMAND-BOUND-6 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
    public void QueueIsBoundedAndClearRestoresCapacity() // TEST-SUPPORT: COMMAND-BOUND-7 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
    {
        var processor = new AbilityCommandProcessor(); // TEST-SUPPORT: COMMAND-BOUND-9 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
        var command = (1, 1L, "caster", 1L, "target", "ability", AbilitySource.Equipment("slot", 1)); // TEST-SUPPORT: COMMAND-BOUND-10 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
        for (var i = 0; i < 32; i++) processor.Enqueue(command); // TEST-SUPPORT: COMMAND-BOUND-11 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
        Assert.That(processor.Count, Is.EqualTo(32)); // TEST-SUPPORT: COMMAND-BOUND-12 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
        Assert.Throws<InvalidOperationException>(() => processor.Enqueue(command)); // TEST-SUPPORT: COMMAND-BOUND-13 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
        processor.Clear(); // TEST-SUPPORT: COMMAND-BOUND-14 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
        processor.Enqueue(command); // TEST-SUPPORT: COMMAND-BOUND-15 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
        Assert.That(processor.Count, Is.EqualTo(1)); // TEST-SUPPORT: COMMAND-BOUND-16 | Tests: sMPGWM.Tests.AbilityCommandProcessorTests.QueueIsBoundedAndClearRestoresCapacity
    }
}
