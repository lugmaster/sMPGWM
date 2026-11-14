using Godot; // TEST-SUPPORT: SHIP-ASSET-001 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: SHIP-ASSET-002 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: SHIP-ASSET-003 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
using sMPGWM.Scripts.VfxSystem; // TEST-SUPPORT: SHIP-ASSET-004 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips

namespace sMPGWM.Tests.ShipSystem; // TEST-SUPPORT: SHIP-ASSET-005 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips

[Category("Godot")] // TEST-SUPPORT: SHIP-ASSET-006 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
public sealed class ShipAssetCatalogueTests // TEST-SUPPORT: SHIP-ASSET-007 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
{
    [Test] // TEST-SUPPORT: SHIP-ASSET-008 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
    public void PreparedAssetsAreSharedAndDisposalPreservesLiveShips() // TEST-SUPPORT: SHIP-ASSET-009 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
    {
        var data = ShipRegistry.GetById("fighter"); // TEST-SUPPORT: SHIP-ASSET-010 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
        using var catalogue = new ShipAssetCatalogue([data]); // TEST-SUPPORT: SHIP-ASSET-011 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
        var first = ShipFactory.Create(data, "prepared-first", Vector2.Zero, 0, Vector2.Zero, new ItemStore(), false, catalogue); // TEST-SUPPORT: SHIP-ASSET-012 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
        ShipController? second = null; // TEST-SUPPORT: SHIP-ASSET-013 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
        try // TEST-SUPPORT: SHIP-ASSET-014 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
        {
            second = ShipFactory.Create(data, "prepared-second", Vector2.Zero, 0, Vector2.Zero, new ItemStore(), false, catalogue); // TEST-SUPPORT: SHIP-ASSET-015 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            var shape = first.GetNode<CollisionShape2D>("ShieldCollider").Shape; // TEST-SUPPORT: SHIP-ASSET-016 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            var texture = first.GetNode<Sprite2D>("ShipVisuals/Sprite2D").Texture; // TEST-SUPPORT: SHIP-ASSET-017 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            Assert.That(second.GetNode<CollisionShape2D>("ShieldCollider").Shape, Is.SameAs(shape)); // TEST-SUPPORT: SHIP-ASSET-018 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            Assert.That(second.GetNode<Sprite2D>("ShipVisuals/Sprite2D").Texture, Is.SameAs(texture)); // TEST-SUPPORT: SHIP-ASSET-019 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            Assert.That(shape, Is.SameAs(catalogue.Get(data).ShieldShape)); // TEST-SUPPORT: SHIP-ASSET-020 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            catalogue.Dispose(); // TEST-SUPPORT: SHIP-ASSET-021 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            Assert.Throws<ObjectDisposedException>(() => catalogue.Get(data)); // TEST-SUPPORT: SHIP-ASSET-022 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            Assert.That(shape.GetRid().IsValid, Is.True); // TEST-SUPPORT: SHIP-ASSET-023 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            Assert.That(texture.GetWidth(), Is.GreaterThan(0)); // TEST-SUPPORT: SHIP-ASSET-024 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            first.GetNode<ShieldVisual>("ShipVisuals/ShieldVisual").Pulse(); // TEST-SUPPORT: SHIP-ASSET-025 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            Assert.That(first.GetNode<ShieldVisual>("ShipVisuals/ShieldVisual").Visible, Is.True); // TEST-SUPPORT: SHIP-ASSET-026 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            using var replacement = new ShipAssetCatalogue([data]); // TEST-SUPPORT: SHIP-ASSET-027 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            Assert.That(replacement.Get(data).ShieldShape, Is.Not.SameAs(shape)); // TEST-SUPPORT: SHIP-ASSET-028 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
        }
        finally // TEST-SUPPORT: SHIP-ASSET-029 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
        {
            first.Loadout.Dispose(); first.Runtime.Dispose(); first.Free(); // TEST-SUPPORT: SHIP-ASSET-030 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
            if (second != null) { second.Loadout.Dispose(); second.Runtime.Dispose(); second.Free(); } // TEST-SUPPORT: SHIP-ASSET-031 | Tests: sMPGWM.Tests.ShipSystem.ShipAssetCatalogueTests.PreparedAssetsAreSharedAndDisposalPreservesLiveShips
        }
    }
}
