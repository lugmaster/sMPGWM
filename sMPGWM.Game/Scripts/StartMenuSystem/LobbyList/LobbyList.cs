using System;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.Steam;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.StartMenuSystem.LobbyList;

public partial class LobbyList : PanelContainer
{
    public event Action<ulong>? SelectionChanged;
    public ulong SelectedLobbyId { get; private set; }
    private ulong _requested;
    private ulong _deadline;
    private VBoxContainer _entries = null!;
    private Label _status = null!;
    private LineEdit _target = null!;

    public override void _Ready()
    {
        var content = GetNode<VBoxContainer>("VBoxContainer");
        _entries = content.GetNode<VBoxContainer>("LobbyScroll/LobbyEntries");
        ClearSelection();
        var toolbar = new FilterToolbar();
        _target = new LineEdit { PlaceholderText = "Private lobby ID from host", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var lookup = new Button { Text = "Look up" };
        toolbar.AddChild(_target);
        toolbar.AddChild(lookup);
        content.AddChild(toolbar);
        content.MoveChild(toolbar, 0);
        _status = new Label { Text = "Enter the host's lobby ID, then select its row." };
        content.AddChild(_status);
        lookup.Pressed += Lookup;
        _target.TextChanged += _ => { _requested = 0; ClearSelection(); };
        SteamworksHelper.Api.LobbyDataUpdateSignal += DataUpdated;
    }

    private void ClearSelection()
    {
        SelectedLobbyId = 0;
        SelectionChanged?.Invoke(0);
        foreach (var child in _entries.GetChildren())
        {
            _entries.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void Lookup()
    {
        _requested = 0;
        ClearSelection();
        if (!ulong.TryParse(_target.Text.Trim(), out var id) || id == 0)
        {
            _status.Text = "Enter a valid numeric lobby ID.";
            return;
        }
        _requested = id;
        _deadline = Time.GetTicksMsec() + 10000;
        _status.Text = "Looking up lobby?";
        try
        {
            if (!SteamworksHelper.Api.RequestLobbyData(checked((long)id))) Fail("Lobby lookup could not start.");
        }
        catch (Exception e) { Fail(e.Message); }
    }

    private void Fail(string reason) { _requested = 0; _status.Text = reason; }

    private void DataUpdated(long success, long lobby, long member)
    {
        if (_requested == 0 || (ulong)lobby != _requested || member != lobby) return;
        _requested = 0;
        try
        {
            if (success != 1 || SteamworksHelper.Api.GetLobbyData(lobby, "game_id") != GameConfig.LobbyGameId ||
                SteamworksHelper.Api.GetLobbyData(lobby, "version") != GameConfig.Version ||
                SteamworksHelper.Api.GetLobbyData(lobby, "state") != "diagnostic" ||
                SteamworksHelper.Api.GetLobbyOwner(lobby) == checked((long)SteamworksHelper.Api.GetSteamId()))
            {
                Fail("Lobby unavailable, incompatible, or hosted by you.");
                return;
            }
            var row = GD.Load<PackedScene>("res://scenes/ui/menu/start_menu/lobby_list_entry.tscn").Instantiate<Button>();
            row.GetNode<Label>("MarginContainer/MainRow/LobbyNameLabel").Text = SteamworksHelper.Api.GetLobbyData(lobby, "name");
            row.GetNode<Label>("MarginContainer/MainRow/HostNameLabel").Text = "Private diagnostic";
            row.GetNode<Label>("MarginContainer/MainRow/PlayerCountLabel").Text = $"{SteamworksHelper.Api.GetNumLobbyMembers(lobby)}/{SteamworksHelper.Api.GetLobbyMemberLimit(lobby)}";
            row.FocusMode = FocusModeEnum.All;
            row.Pressed += () => { SelectedLobbyId = row.ButtonPressed ? checked((ulong)lobby) : 0; SelectionChanged?.Invoke(SelectedLobbyId); };
            _entries.AddChild(row);
            _status.Text = "Select the lobby row, then Join Steam Game.";
        }
        catch (Exception e) { Fail(e.Message); }
    }

    public override void _Process(double delta)
    {
        if (_requested != 0 && Time.GetTicksMsec() >= _deadline) Fail("Lobby lookup timed out. Try again.");
    }

    public override void _ExitTree()
    {
        _requested = 0;
        SteamworksHelper.Api.LobbyDataUpdateSignal -= DataUpdated;
        SelectionChanged = null;
    }
}
