using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class ItemInteractionControllerTests
{
    private InventoryImpl _inventory = null!;
    private EquipmentImpl _equipment = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private ItemInteractionController _controller = null!;
    private ItemCatalog _catalog = null!;

    [SetUp]
    public void SetUp()
    {
        _catalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(new ItemDefinitionId("engine"), "Engine", ItemCategories.Engine),
            new ItemDefinition(new ItemDefinitionId("weapon"), "Weapon", ItemCategories.LightWeapon),
            new ItemDefinition(new ItemDefinitionId("potion"), "Potion", ItemCategories.Consumable, maximumStackSize: 10)
        });
        _inventory = new InventoryImpl(_catalog);
        _equipment = new EquipmentImpl(_catalog);
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        _controller = new ItemInteractionController(_transfers);
        _controller.SetSession(_transfers.OpenEditSession());
    }

    [TearDown]
    public void TearDown()
    {
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }

    private void Seed(params (string Definition, int Count)[] entries)
    {
        var slots = _inventory.Snapshot.Slots.ToArray();
        for (var index = 0; index < entries.Length; index++)
            slots[index] = _catalog.CreateEntity(new ItemDefinitionId(entries[index].Definition), entries[index].Count);
        _inventory.Install(_inventory.PrepareChange(slots));
    }

    private ItemCellBinding Bag(int address) => new(ItemLocation.Inventory(address),
        _inventory.Snapshot.Slots[address], _inventory.Revision);
    private ItemCellBinding Slot(string key) => new(ItemLocation.Equipment(key),
        _equipment.Snapshot.Slots[_equipment.FindSlot(key)], _equipment.Revision);
    private void Click(ItemCellBinding target, long mapping = 0) =>
        _controller.Click(target, _inventory.Revision, _equipment.Revision, mapping);

    [Test]
    public void NativeDrag_HoverIsReadOnlyAndTokenCommitsOnlyOnce()
    {
        Seed(("engine", 1));
        var before = _inventory.Snapshot;
        var token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
        Assert.That(token, Is.Not.EqualTo(Guid.Empty));
        for (var index = 0; index < 3; index++)
        {
            Assert.That(_controller.ValidateDrag(token, Slot("hull")), Is.EqualTo(TransferResult.IncompatiblePlacement));
            Assert.That(_controller.ValidateDrag(token, Slot("engine")), Is.EqualTo(TransferResult.Success));
        }
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
        Assert.That(_controller.Drop(token, Slot("engine")), Is.EqualTo(TransferResult.Success));
        Assert.That(_controller.Drop(token, Bag(1)), Is.Null);
        Assert.That(_inventory.Revision, Is.EqualTo(before.Revision + 1));
        Assert.That(_controller.IsCarrying, Is.False);
    }

    [TestCase("hull", TransferResult.IncompatiblePlacement)]
    [TestCase("engine", TransferResult.Success)]
    public void NativeDrag_AlwaysEndsCarrying(string slot, TransferResult expected)
    {
        Seed(("engine", 1));
        var token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
        Assert.That(_controller.Drop(token, Slot(slot)), Is.EqualTo(expected));
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot.Slots[0] != null, Is.EqualTo(expected != TransferResult.Success));
    }

    [TestCase("cancel")]
    [TestCase("session")]
    [TestCase("mapping")]
    [TestCase("revision")]
    public void NativeDrag_InvalidatedTokensCannotCommit(string reason)
    {
        Seed(("engine", 1));
        var token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
        switch (reason)
        {
            case "cancel": _controller.Cancel(); break;
            case "session": _controller.SetSession(_transfers.OpenEditSession()); break;
            case "mapping": _controller.Refresh(_inventory.Revision, _equipment.Revision, 1); break;
            case "revision": _inventory.Add(new ItemDefinitionId("engine"), 1); break;
        }
        var before = _inventory.Snapshot;
        Assert.That(_controller.Drop(token, Slot("engine")), Is.Not.EqualTo(TransferResult.Success));
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void NativeDrag_RejectsForeignAndRevokedTokensAndRevalidatesTarget()
    {
        Seed(("potion", 7), ("potion", 8));
        var token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
        Assert.That(_controller.ValidateDrag(Guid.NewGuid(), Bag(1)), Is.Null);
        Assert.That(_controller.Drop(Guid.NewGuid(), Bag(1)), Is.Null);
        Assert.That(_controller.Drop(token, Bag(1)), Is.EqualTo(TransferResult.Success));
        Assert.That(_inventory.Snapshot.Slots[0]!.Count, Is.EqualTo(5));
        Assert.That(_inventory.Snapshot.Slots[1]!.Count, Is.EqualTo(10));
        token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
        Assert.That(_controller.Drop(token, Bag(2) with { Revision = -1 }), Is.EqualTo(TransferResult.StaleRevision));
        token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
        _transfers.RevokeEditSession();
        Assert.That(_controller.ValidateDrag(token, Bag(2)), Is.EqualTo(TransferResult.InvalidSession));
        Assert.That(_controller.IsCarrying, Is.True);
        Assert.That(_controller.Drop(token, Bag(2)), Is.EqualTo(TransferResult.InvalidSession));
        Assert.That(_controller.IsCarrying, Is.False);
    }

    [Test]
    public void PickupValidateAndEquip_PreserveSourceUntilCommit()
    {
        Seed(("engine", 1));
        var source = Bag(0);
        var before = _inventory.Snapshot;
        Click(source);
        for (var index = 0; index < 3; index++)
            Assert.That(_controller.Validate(Slot("engine")), Is.EqualTo(TransferResult.Success));
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
        Click(Slot("engine"));
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_equipment.Snapshot.Slots[0], Is.SameAs(source.Entity));
        Assert.That(_inventory.Snapshot.Slots[0], Is.Null);
        Click(Slot("engine"));
        Click(Bag(10));
        Assert.That(_inventory.Snapshot.Slots[10], Is.SameAs(source.Entity));
    }

    [Test]
    public void EmptyPickupAndSourceClickAndExplicitCancel_DoNotMutate()
    {
        Click(Bag(0));
        Assert.That(_controller.IsCarrying, Is.False);
        Seed(("engine", 1));
        var before = _inventory.Snapshot;
        Click(Bag(0));
        Click(Bag(0));
        Assert.That(_controller.IsCarrying, Is.False);
        Click(Bag(0));
        _controller.Cancel();
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void InvalidTargetRetainsCarrying_AndNextValidTargetWorks()
    {
        Seed(("engine", 1));
        var before = _inventory.Snapshot;
        Click(Bag(0));
        Click(Slot("hull"));
        Assert.That(_controller.Feedback, Is.EqualTo(TransferResult.IncompatiblePlacement));
        Assert.That(_controller.IsCarrying, Is.True);
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
        Click(Slot("engine"));
        Assert.That(_controller.IsCarrying, Is.False);
    }

    [TestCase(8, 5, false)]
    [TestCase(2, 0, false)]
    [TestCase(10, 7, true)]
    public void MergeEndsCarryingExceptFullStack(int destinationCount, int remaining, bool carrying)
    {
        Seed(("potion", 7), ("potion", destinationCount));
        var source = Bag(0).Entity!;
        var destination = Bag(1).Entity!;
        Click(Bag(0));
        Click(Bag(1));
        Assert.That(_controller.IsCarrying, Is.EqualTo(carrying));
        Assert.That(_inventory.Snapshot.Slots[0]?.Count ?? 0, Is.EqualTo(remaining));
        Assert.That(_inventory.Snapshot.Slots[1]!.Id, Is.EqualTo(destination.Id));
        if (remaining > 0) Assert.That(_inventory.Snapshot.Slots[0]!.Id, Is.EqualTo(source.Id));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ChangedRevisionOrMapping_CancelsWithoutPickingUpTarget(bool revision)
    {
        Seed(("engine", 1), ("engine", 1));
        Click(Bag(0));
        if (revision) _inventory.Add(new ItemDefinitionId("engine"), 1);
        var before = _inventory.Snapshot;
        Click(Bag(1), revision ? 0 : 1);
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
    }

    [Test]
    public void RevokedSessionAndStaleDisplayedTarget_CancelWithoutMutation()
    {
        Seed(("engine", 1));
        Click(Bag(0));
        _transfers.RevokeEditSession();
        Click(Bag(2));
        Assert.That(_controller.IsCarrying, Is.False);
        _controller.SetSession(_transfers.OpenEditSession());
        Click(Bag(0));
        var stale = Bag(2) with { Revision = -1 };
        Click(stale);
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot.Slots[0], Is.Not.Null);
    }

    [Test]
    public void ReopenSessionAndEquipmentRefresh_ClearPendingSource()
    {
        Seed(("engine", 1));
        Click(Bag(0));
        _controller.SetSession(_transfers.OpenEditSession());
        Assert.That(_controller.IsCarrying, Is.False);
        Click(Bag(0));
        _controller.Refresh(_inventory.Revision, _equipment.Revision + 1, 0);
        Assert.That(_controller.IsCarrying, Is.False);
        _controller.SetSession(Guid.Empty);
        Click(Bag(0));
        Assert.That(_controller.IsCarrying, Is.False);
    }
    private ItemHotbarState ConfigureHotbar()
    {
        var state = new ItemHotbarState(new HotbarBinding[] { new ActionHotbarBinding("7") });
        var session = _transfers.OpenEditSession();
        state.SetEditSession(session);
        _controller.SetSession(session);
        _controller.BindHotbar(state, new ItemHotbarAdapter(state, _inventory, _equipment, _transfers));
        return state;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnifiedPlacement_AssignsConsumableWithoutMovingItems(bool drag)
    {
        Seed(("potion", 7));
        using var hotbar = ConfigureHotbar();
        var before = _inventory.Snapshot;
        var target = ItemCellBinding.Hotbar(1, hotbar.Revision);
        if (drag)
        {
            var token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
            Assert.That(_controller.ValidateDrag(token, target), Is.EqualTo(TransferResult.Success));
            Assert.That(_controller.Drop(token, target), Is.EqualTo(TransferResult.Success));
            Assert.That(_controller.Drop(token, target), Is.Null);
        }
        else { Click(Bag(0)); Click(target); }
        Assert.That(hotbar.Entries[1], Is.EqualTo(new ConsumableHotbarBinding(new("potion"))));
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
        Assert.That(_controller.Mode, Is.EqualTo(ItemInteractionController.PlacementMode.Idle));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnifiedPlacement_MovesAndSwapsHotbarActions(bool drag)
    {
        Seed(("potion", 1));
        using var hotbar = ConfigureHotbar();
        Click(Bag(0));
        Click(ItemCellBinding.Hotbar(1, hotbar.Revision));
        var source = ItemCellBinding.Hotbar(0, hotbar.Revision);
        var target = ItemCellBinding.Hotbar(1, hotbar.Revision);
        if (drag)
        {
            var token = _controller.BeginDrag(source, _inventory.Revision, _equipment.Revision, 0);
            Assert.That(_controller.ValidateDrag(token, Bag(2)), Is.EqualTo(TransferResult.IncompatiblePlacement));
            Assert.That(_controller.Drop(token, target), Is.EqualTo(TransferResult.Success));
        }
        else { Click(source); Click(target); }
        Assert.That(hotbar.Entries[0], Is.TypeOf<ConsumableHotbarBinding>());
        Assert.That(hotbar.Entries[1], Is.EqualTo(new ActionHotbarBinding("7")));
        Click(ItemCellBinding.Hotbar(1, hotbar.Revision));
        Click(ItemCellBinding.Hotbar(2, hotbar.Revision));
        Assert.That(hotbar.Entries[1], Is.Null);
        Assert.That(hotbar.Entries[2], Is.EqualTo(new ActionHotbarBinding("7")));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnifiedPlacement_RejectsStaleHotbarTargetAndRevokedSession(bool drag)
    {
        Seed(("potion", 3));
        using var hotbar = ConfigureHotbar();
        var target = ItemCellBinding.Hotbar(1, -1);
        if (drag)
        {
            var token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
            Assert.That(_controller.Drop(token, target), Is.EqualTo(TransferResult.StaleRevision));
        }
        else { Click(Bag(0)); Click(target); }
        Assert.That(hotbar.Entries[1], Is.Null);
        Assert.That(_controller.IsCarrying, Is.False);
        _controller.SetSession(Guid.Empty);
        Click(ItemCellBinding.Hotbar(0, hotbar.Revision));
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_controller.Clear(ItemCellBinding.Hotbar(0, hotbar.Revision)), Is.EqualTo(HotbarResult.InvalidSession));
    }

    [Test]
    public void NativeDrag_NewerBagTargetCannotAuthorizeAnOlderSource()
    {
        Seed(("potion", 3));
        var token = _controller.BeginDrag(Bag(0), _inventory.Revision, _equipment.Revision, 0);
        _inventory.Add(new("potion"), 1);
        var before = _inventory.Snapshot;
        Assert.That(_controller.Drop(token, Bag(1)), Is.EqualTo(TransferResult.StaleRevision));
        Assert.That(_inventory.Snapshot, Is.SameAs(before));
    }

    [TestCase("disposed", TransferResult.Disposed, false)]
    [TestCase("location", TransferResult.InvalidLocation, false)]
    [TestCase("publication", TransferResult.PublicationInProgress, true)]
    [TestCase("session", TransferResult.InvalidSession, false)]
    [TestCase("revision", TransferResult.StaleRevision, false)]
    [TestCase("entity", TransferResult.StaleRevision, false)]
    [TestCase("ineligible", TransferResult.IncompatiblePlacement, true)]
    public void HotbarSourceRejectionsApplyPickupPolicy(string reason, TransferResult expected, bool retained)
    {
        Seed(("potion", 3), ("engine", 1));
        using var hotbar = ConfigureHotbar();
        var source = reason == "ineligible" ? Bag(1) : Bag(0);
        if (reason == "location") source = source with { Location = ItemLocation.Inventory(_inventory.Capacity) };
        if (reason == "entity") source = source with { Entity = _catalog.CreateEntity(new("potion"), 3) };
        void Invalidate()
        {
            switch (reason)
            {
                case "disposed": _transfers.Dispose(); break;
                case "publication": _inventory.SetPublicationInProgress(true); break;
                case "session": _transfers.RevokeEditSession(); break;
                case "revision": _inventory.Add(new("potion"), 1); break;
            }
        }
        Click(source);
        Invalidate();
        var inventory = _inventory.Snapshot;
        var equipment = _equipment.Snapshot;
        var entries = hotbar.Entries;
        var revision = hotbar.Revision;
        var notifications = 0;
        _inventory.Changed += _ => notifications++;
        _equipment.Changed += _ => notifications++;
        hotbar.Changed += () => notifications++;
        var target = ItemCellBinding.Hotbar(1, revision);
        Assert.That(_controller.Validate(target), Is.EqualTo(expected));
        Assert.That(_controller.IsCarrying, Is.EqualTo(retained));

        // Start a fresh click attempt to exercise commit cancellation independently of hover.
        _controller.Cancel();
        _controller.Click(source, source.Revision, _equipment.Revision, 0);
        Click(target);
        Assert.That(_controller.IsCarrying, Is.EqualTo(retained));
        if (retained) Assert.That(_controller.Feedback, Is.EqualTo(expected));

        _controller.Cancel();
        var token = _controller.BeginDrag(source, source.Revision, _equipment.Revision, 0);
        Assert.That(_controller.Drop(token, target), Is.EqualTo(expected));
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot, Is.SameAs(inventory));
        Assert.That(_equipment.Snapshot, Is.SameAs(equipment));
        Assert.That(hotbar.Entries, Is.SameAs(entries));
        Assert.That(hotbar.Revision, Is.EqualTo(revision));
        Assert.That(notifications, Is.Zero);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void HotbarNoOpsRetainClickPickupAndEndNativeDrag(bool weapon, bool move)
    {
        Seed(("potion", 3), ("potion", 4), ("weapon", 1));
        using var hotbar = ConfigureHotbar();
        if (weapon)
        {
            Click(Bag(2));
            Click(Slot("light_weapon_1"));
        }
        var itemSource = weapon ? Slot("light_weapon_1") : Bag(0);
        Click(itemSource);
        Click(ItemCellBinding.Hotbar(1, hotbar.Revision));
        Click(itemSource);
        Click(ItemCellBinding.Hotbar(2, hotbar.Revision));
        var source = move ? ItemCellBinding.Hotbar(1, hotbar.Revision) : weapon ? itemSource : Bag(1);
        var target = ItemCellBinding.Hotbar(2, hotbar.Revision);
        var inventory = _inventory.Snapshot;
        var equipment = _equipment.Snapshot;
        var entries = hotbar.Entries;
        var revision = hotbar.Revision;
        var notifications = 0;
        _inventory.Changed += _ => notifications++;
        _equipment.Changed += _ => notifications++;
        hotbar.Changed += () => notifications++;
        Click(source);
        Assert.That(_controller.Validate(source), Is.EqualTo(TransferResult.NoChange));
        Assert.That(_controller.Validate(target), Is.EqualTo(TransferResult.NoChange));
        Click(target);
        Assert.That(_controller.Feedback, Is.EqualTo(TransferResult.NoChange));
        Assert.That(_controller.IsCarrying, Is.True);
        Click(source);
        Assert.That(_controller.IsCarrying, Is.False);
        var token = _controller.BeginDrag(source, _inventory.Revision, _equipment.Revision, 0);
        Assert.That(_controller.ValidateDrag(token, target), Is.EqualTo(TransferResult.NoChange));
        Assert.That(_controller.Drop(token, target), Is.EqualTo(TransferResult.NoChange));
        Assert.That(_controller.IsCarrying, Is.False);
        Assert.That(_inventory.Snapshot, Is.SameAs(inventory));
        Assert.That(_equipment.Snapshot, Is.SameAs(equipment));
        Assert.That(hotbar.Entries, Is.SameAs(entries));
        Assert.That(hotbar.Revision, Is.EqualTo(revision));
        Assert.That(notifications, Is.Zero);
    }

}
