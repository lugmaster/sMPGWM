using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

public partial class WeaponStatusPanel : VBoxContainer
{
    private readonly Row[] _rows = new Row[9];
    private NetworkManager? _network;
    private double _refresh;
    private Button _reload = null!;

    public void Bind(NetworkManager? network) => _network = network;

    public override void _Ready()
    {
        AddChild(new Label { Text = "Fire groups and supply" });
        for (var i = 0; i < _rows.Length; i++)
        {
            var box = new HBoxContainer();
            var label = new Label { CustomMinimumSize = new(130, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var group = new OptionButton { CustomMinimumSize = new(110, 0) };
            group.AddItem("Disabled", 0);
            group.AddItem("Group 1", 1);
            group.AddItem("Group 2", 2);
            var status = new Label { CustomMinimumSize = new(130, 0) };
            var progress = new ProgressBar { CustomMinimumSize = new(70, 12), ShowPercentage = false, MaxValue = 1 };
            var row = new Row(label, group, status, progress, box);
            group.ItemSelected += selected =>
            {
                if (_network?.WeaponBaselineReady == true && row.Item != default)
                    _network.RequestWeaponGroup(row.Slot, row.Item, (byte)selected);
            };
            box.AddChild(label); box.AddChild(group); box.AddChild(status); box.AddChild(progress);
            AddChild(box);
            _rows[i] = row;
        }
        _reload = new Button { Text = "Reload ammunition weapons" };
        _reload.Pressed += () => _network?.RequestWeaponReload();
        AddChild(_reload);
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        _refresh -= delta;
        if (_refresh > 0) return;
        _refresh = .1;
        var ship = _network?.World.LocalPlayer?.Participant?.ControlledShip;
        var ready = ship != null && _network!.WeaponBaselineReady;
        _reload.Disabled = !ready || !ship!.Runtime.IsAlive;
        var rowIndex = 0;
        if (ready)
        {
            var tick = _network!.Synchronization?.PresentationTick ?? ship!.Runtime.AuthoritativeTick;
            for (var slot = 0; slot < ship!.Loadout.Weapons.Bindings.Count && rowIndex < _rows.Length; slot++)
            {
                var binding = ship.Loadout.Weapons.Bindings[slot];
                if (binding == null || !ship.Loadout.Store.Weapons.TryGet(binding.ItemId, out var state)) continue;
                var row = _rows[rowIndex++];
                row.Box.Visible = true;
                row.Item = binding.ItemId;
                row.Slot = ship.Equipment.Definitions[slot].Key;
                row.Name.Text = ship.Equipment.Snapshot.Slots[slot]?.Definition.DisplayName ?? row.Slot;
                row.Group.Select(ship.Loadout.Weapons.GetGroup(slot));
                row.Group.Disabled = !ship.Runtime.IsAlive;
                var stats = binding.Definition.Stats;
                row.Progress.Visible = state.Reloading;
                if (state.Reloading)
                {
                    row.Status.Text = $"Reloading {state.Rounds}/{stats.MagazineCapacity}";
                    row.Progress.Value = System.Math.Clamp((tick - state.ReloadStartTick) / System.Math.Max(1d, state.ReloadDeadline - state.ReloadStartTick), 0, 1);
                }
                else if (!ship.Runtime.IsAlive) row.Status.Text = "Disabled";
                else if (!ship.Runtime.PoweredSystemsAvailable) row.Status.Text = "No power";
                else if (binding.Definition.SupplyMode == WeaponSupplyMode.Ammunition) row.Status.Text = $"{state.Rounds}/{stats.MagazineCapacity}";
                else row.Status.Text = ship.Runtime.Energy.Current < stats.EnergyPerShot ? "Insufficient energy" : $"{stats.EnergyPerShot:0.#} energy / shot";
            }
        }
        for (; rowIndex < _rows.Length; rowIndex++) { _rows[rowIndex].Box.Visible = false; _rows[rowIndex].Item = default; }
    }

    internal sealed class Row(Label name, OptionButton group, Label status, ProgressBar progress, HBoxContainer box)
    {
        public readonly Label Name = name;
        public readonly OptionButton Group = group;
        public readonly Label Status = status;
        public readonly ProgressBar Progress = progress;
        public readonly HBoxContainer Box = box;
        public string Slot = "";
        public ItemEntityId Item;
    }
}
