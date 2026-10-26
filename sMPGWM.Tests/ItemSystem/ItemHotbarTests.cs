using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Tests.ItemSystem;

[TestFixture]
public class ItemHotbarTests
{
    private ItemCatalog _catalog = null!;
    private InventoryImpl _inventory = null!;
    private EquipmentImpl _equipment = null!;
    private InventoryEquipmentTransferService _transfers = null!;
    private ItemHotbarState _state = null!;
    private ItemHotbarAdapter _adapter = null!;
    private Guid _session;

    [SetUp]
    public void SetUp()
    {
        var categories = ItemCategories.CreateDefault();
        _catalog = new ItemCatalog(categories, categories.All.Select(category =>
            new ItemDefinition(new ItemDefinitionId(category.Id.Value), category.DisplayName, category.Id,
                maximumStackSize: category.CanStack ? 10 : null)).Append(
            new ItemDefinition(new ItemDefinitionId("other_potion"), "Other potion", ItemCategories.Consumable)));
        _inventory = new InventoryImpl(_catalog);
        _equipment = new EquipmentImpl(_catalog);
        _transfers = new InventoryEquipmentTransferService(_inventory, _equipment);
        _state = new ItemHotbarState(Enumerable.Range(0, 10).Select(index => new ActionHotbarBinding(index.ToString())));
        _adapter = new ItemHotbarAdapter(_state, _inventory, _equipment, _transfers);
        _session = _transfers.OpenEditSession();
        _state.SetEditSession(_session);
        var slots = _inventory.Snapshot.Slots.ToArray();
        slots[0] = _catalog.CreateEntity(new ItemDefinitionId("consumable"), 7);
        slots[1] = _catalog.CreateEntity(new ItemDefinitionId("consumable"), 8);
        slots[2] = _catalog.CreateEntity(new ItemDefinitionId("other_potion"), 9);
        slots[3] = _catalog.CreateEntity(new ItemDefinitionId("light_weapon"));
        slots[4] = _catalog.CreateEntity(new ItemDefinitionId("engine"));
        _inventory.Install(_inventory.PrepareChange(slots));
    }

    [TearDown]
    public void TearDown()
    {
        _state.Dispose();
        _transfers.Dispose();
        _equipment.Dispose();
        _inventory.Dispose();
    }

    private HotbarResult Assign(int source, int position = 0) => _adapter.Assign(_session, _state.Revision, position,
        ItemLocation.Inventory(source), _inventory.Snapshot.Slots[source]!.Id, _inventory.Revision, _equipment.Revision);

    private TransferResult Equip(int source) => _transfers.Transfer(new TransferRequest(_session, ItemLocation.Inventory(source),
        _inventory.Snapshot.Slots[source]!.Id, ItemLocation.Equipment("light_weapon_1"), _equipment.Snapshot.Slots[4]?.Id,
        _inventory.Revision, _equipment.Revision));

    [Test]
    public void ConsumableAssignmentAggregatesOnlyMatchingDefinitionAndChangesNoOwnership()
    {
        var inventory = _inventory.Snapshot;
        var equipment = _equipment.Snapshot;
        Assert.That(Assign(0), Is.EqualTo(HotbarResult.Success));
        Assert.That(_adapter.Resolve(0).Quantity, Is.EqualTo(15L));
        Assert.That(_inventory.Snapshot, Is.SameAs(inventory));
        Assert.That(_equipment.Snapshot, Is.SameAs(equipment));
        Assert.That(Assign(1, 1), Is.EqualTo(HotbarResult.Success));
        Assert.That(_state.Entries[0], Is.EqualTo(_state.Entries[1]));
        var snapshot = _state.Entries;
        var revision = _state.Revision;
        Assert.That(_state.Move(_session, revision, 0, 1), Is.EqualTo(HotbarResult.NoChange));
        Assert.That(_state.Entries, Is.SameAs(snapshot));
        Assert.That(_state.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void AggregateUsesLongAndRetainsZeroQuantityDefinition()
    {
        Assign(0);
        var slots = _inventory.Snapshot.Slots.ToArray();
        slots[0] = null;
        slots[1] = null;
        _inventory.Install(_inventory.PrepareChange(slots));
        Assert.That(_adapter.Resolve(0), Is.EqualTo(new HotbarAvailability(new ItemDefinitionId("consumable"), 0, "Consumable", false)));
        Assert.That(_state.Entries[0], Is.TypeOf<ConsumableHotbarBinding>());
        var largeCatalog = new ItemCatalog(ItemCategories.CreateDefault(), new[]
        {
            new ItemDefinition(new ItemDefinitionId("large"), "Large", ItemCategories.Consumable)
        });
        using var inventory = new InventoryImpl(largeCatalog);
        using var equipment = new EquipmentImpl(largeCatalog);
        using var transfers = new InventoryEquipmentTransferService(inventory, equipment);
        using var state = new ItemHotbarState();
        var session = transfers.OpenEditSession();
        state.SetEditSession(session);
        inventory.Add(new ItemDefinitionId("large"), (long)int.MaxValue * 2);
        var adapter = new ItemHotbarAdapter(state, inventory, equipment, transfers);
        adapter.Assign(session, 0, 0, ItemLocation.Inventory(0), inventory.Snapshot.Slots[0]!.Id, inventory.Revision, 0);
        Assert.That(adapter.Resolve(0).Quantity, Is.EqualTo((long)int.MaxValue * 2));
    }

    [TestCase(3)]
    [TestCase(4)]
    public void InventoryWeaponsAndUnsupportedCategoriesAreRejected(int address)
    {
        var entries = _state.Entries;
        Assert.That(Assign(address), Is.EqualTo(HotbarResult.IneligibleSource));
        Assert.That(_state.Entries, Is.SameAs(entries));
    }

    [Test]
    public void WeaponSlotLinkTracksReplacementEmptyAndRefill()
    {
        Equip(3);
        var first = _equipment.Snapshot.Slots[4]!;
        Assert.That(_adapter.Assign(_session, 0, 0, ItemLocation.Equipment("light_weapon_1"), first.Id,
            _inventory.Revision, _equipment.Revision), Is.EqualTo(HotbarResult.Success));
        Assert.That(_state.Entries[0], Is.EqualTo(new WeaponHotbarBinding("light_weapon_1")));
        _transfers.Transfer(new TransferRequest(_session, ItemLocation.Equipment("light_weapon_1"), first.Id,
            ItemLocation.Inventory(3), null, _inventory.Revision, _equipment.Revision));
        Assert.That(_adapter.Resolve(0).Available, Is.False);
        Assert.That(_adapter.Resolve(0).Label, Is.EqualTo("Light Weapon Slot 1"));
        _inventory.Add(new ItemDefinitionId("light_weapon"), 1);
        Equip(5);
        Assert.That(_adapter.Resolve(0).Available, Is.True);
        Assert.That(_equipment.Snapshot.Slots[4]!.Id, Is.Not.EqualTo(first.Id));
    }

    [Test]
    public void MoveSwapsEntriesClearLeavesEmptyAndUnavailableLinksStillMove()
    {
        Assign(0);
        Assert.That(_state.Move(_session, _state.Revision, 0, 1), Is.EqualTo(HotbarResult.Success));
        Assert.That(_state.Entries[0], Is.EqualTo(new ActionHotbarBinding("1")));
        Assert.That(_state.Entries[1], Is.TypeOf<ConsumableHotbarBinding>());
        _state.Clear(_session, _state.Revision, 0);
        _state.Move(_session, _state.Revision, 1, 0);
        Assert.That(_state.Entries[1], Is.Null);
        _state.Clear(_session, _state.Revision, 0);
        Assert.That(_state.Entries[0], Is.Null);
    }

    [Test]
    public void RejectedEditsAndNoOpsDoNotPublish()
    {
        var changes = 0;
        _state.Changed += () => changes++;
        var entries = _state.Entries;
        Assert.That(_state.Clear(Guid.NewGuid(), 0, 0), Is.EqualTo(HotbarResult.InvalidSession));
        Assert.That(_state.Clear(_session, 1, 0), Is.EqualTo(HotbarResult.StaleRevision));
        Assert.That(_state.Clear(_session, 0, 10), Is.EqualTo(HotbarResult.InvalidPosition));
        Assert.That(_state.Move(_session, 0, 0, 0), Is.EqualTo(HotbarResult.NoChange));
        Assert.That(_state.Entries, Is.SameAs(entries));
        Assert.That(changes, Is.Zero);
        _state.Clear(_session, 0, 0);
        Assert.That(_state.Clear(_session, 1, 0), Is.EqualTo(HotbarResult.NoChange));
        Assert.That(changes, Is.EqualTo(1));
        _state.SetEditSession(Guid.Empty);
        Assert.That(_state.Move(_session, 1, 1, 0), Is.EqualTo(HotbarResult.InvalidSession));
    }

    [Test]
    public void AssignmentRejectsStaleSourceTargetAndSession()
    {
        var entity = _inventory.Snapshot.Slots[0]!;
        Assert.That(_adapter.Assign(_session, 0, 0, ItemLocation.Inventory(0), entity.Id, 0, 0), Is.EqualTo(HotbarResult.StaleRevision));
        Assert.That(_adapter.Assign(_session, 0, 0, ItemLocation.Inventory(0), ItemEntityId.Create(), _inventory.Revision, 0), Is.EqualTo(HotbarResult.StaleRevision));
        Assign(0);
        Assert.That(_adapter.Assign(_session, 0, 1, ItemLocation.Inventory(0), entity.Id, _inventory.Revision, 0), Is.EqualTo(HotbarResult.StaleRevision));
        _transfers.RevokeEditSession();
        Assert.That(Assign(0, 2), Is.EqualTo(HotbarResult.InvalidSession));
    }

    [Test]
    public void EmptyWeaponSlotCannotCreateAnAssignment()
    {
        Assert.That(_adapter.Assign(_session, 0, 0, ItemLocation.Equipment("light_weapon_1"),
            ItemEntityId.Create(), _inventory.Revision, _equipment.Revision), Is.EqualTo(HotbarResult.StaleRevision));
        Assert.That(_state.Revision, Is.Zero);
    }

    [Test]
    public void MergeAndMovesPreserveDefinitionAggregate()
    {
        Assign(0);
        _transfers.Transfer(new TransferRequest(_session, ItemLocation.Inventory(0), _inventory.Snapshot.Slots[0]!.Id,
            ItemLocation.Inventory(1), _inventory.Snapshot.Slots[1]!.Id, _inventory.Revision, _equipment.Revision));
        Assert.That(_adapter.Resolve(0).Quantity, Is.EqualTo(15L));
        _transfers.Transfer(new TransferRequest(_session, ItemLocation.Inventory(1), _inventory.Snapshot.Slots[1]!.Id,
            ItemLocation.Inventory(7), null, _inventory.Revision, _equipment.Revision));
        Assert.That(_adapter.Resolve(0).Quantity, Is.EqualTo(15L));
    }

    [Test]
    public void UnavailableLinksMoveAndClearButCannotActivate()
    {
        Equip(3);
        var weapon = _equipment.Snapshot.Slots[4]!;
        _adapter.Assign(_session, 0, 0, ItemLocation.Equipment("light_weapon_1"), weapon.Id, _inventory.Revision, _equipment.Revision);
        _transfers.Transfer(new TransferRequest(_session, ItemLocation.Equipment("light_weapon_1"), weapon.Id,
            ItemLocation.Inventory(3), null, _inventory.Revision, _equipment.Revision));
        _state.Move(_session, _state.Revision, 0, 1);
        var activations = 0;
        Assert.That(_adapter.Activate(1, _ => activations++, _ => activations++), Is.EqualTo(HotbarResult.Unavailable));
        Assert.That(activations, Is.Zero);
        Assert.That(_state.Clear(_session, _state.Revision, 1), Is.EqualTo(HotbarResult.Success));
    }

    [Test]
    public void ReopeningRevokesOldEditsAndDisposalReleasesState()
    {
        var oldSession = _session;
        _session = _transfers.OpenEditSession();
        _state.SetEditSession(_session);
        Assert.That(_state.Clear(oldSession, 0, 0), Is.EqualTo(HotbarResult.InvalidSession));
        Assert.That(_state.Clear(_session, 0, 0), Is.EqualTo(HotbarResult.Success));
        _state.Dispose();
        Assert.That(_state.Move(_session, 1, 1, 0), Is.EqualTo(HotbarResult.Disposed));
    }

    [Test]
    public void ActivationPreservesOriginalActionIdentityAndEmitsTypedItemRequest()
    {
        _state.Move(_session, 0, 0, 5);
        var activated = "";
        ItemHotbarActivationRequest? request = null;
        _adapter.Activate(5, key => activated = key, item => request = item);
        Assert.That(activated, Is.EqualTo("0"));
        Assign(0, 5);
        activated = "";
        _adapter.Activate(5, key => activated = key, item => request = item);
        Assert.That(activated, Is.Empty);
        Assert.That(request!.DefinitionId, Is.EqualTo(new ItemDefinitionId("consumable")));
        Assert.That(request.Quantity, Is.EqualTo(15L));
        Assert.That(_inventory.Snapshot.Slots[0]!.Count, Is.EqualTo(7));
    }

    [Test]
    public void PublicationIsolatesFailuresAndRejectsReentrancy()
    {
        var observed = false;
        _state.Changed += () => throw new InvalidOperationException();
        _state.Changed += () =>
        {
            Assert.That(_state.Clear(_session, _state.Revision, 1), Is.EqualTo(HotbarResult.PublicationInProgress));
            observed = true;
        };
        Assert.That(Assign(0), Is.EqualTo(HotbarResult.Success));
        Assert.That(observed, Is.True);
        Assert.That(_state.Entries, Is.AssignableTo<System.Collections.IList>());
        Assert.Throws<NotSupportedException>(() => ((IList<HotbarBinding?>)_state.Entries)[0] = null);
    }

    [Test]
    public void NewCategoriesDefaultToNoHotbarEligibility()
    {
        var category = new ItemCategoryId("extra");
        Assert.That(ItemHotbarPolicy.IsConsumable(category), Is.False);
        Assert.That(ItemHotbarPolicy.IsWeapon(category), Is.False);
    }
}
