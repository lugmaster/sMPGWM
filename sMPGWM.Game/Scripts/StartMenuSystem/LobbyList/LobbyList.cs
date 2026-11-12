using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.SteamSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;
using FilterToolbar = sMPGWM.Scripts.UtilitySystem.UiComponents.FilterToolbar;
using SteamworksHelper = sMPGWM.Scripts.SteamSystem.SteamworksHelper;

namespace sMPGWM.Scripts.StartMenuSystem.LobbyList;

public partial class LobbyList : PanelContainer
{
    public event Action<ulong>? SelectionChanged;
    public ulong SelectedLobbyId { get; private set; }
    private bool _searching;
    private ulong _deadline;
    private VBoxContainer _entries = null!;
    private Label _status = null!;
    private Button _refresh = null!;
    private readonly ButtonGroup _selection = new() { AllowUnpress = true };
    private LobbyDiscovery _discovery = null!;

    public override void _Ready()
    {
        var content = GetNode<VBoxContainer>("VBoxContainer");
        _entries = content.GetNode<VBoxContainer>("LobbyScroll/LobbyEntries");
        _discovery = new LobbyDiscovery(GameConfig.LobbyGameId, GameConfig.LobbyMode, GameConfig.Version);
        var toolbar = new FilterToolbar();
        _refresh = new Button { Text = "Refresh games" };
        toolbar.AddChild(_refresh);
        content.AddChild(toolbar);
        content.MoveChild(toolbar, 0);
        _status = new Label();
        content.AddChild(_status);
        _refresh.Pressed += Refresh;
        Refresh();
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

    private void Refresh()
    {
        if (_searching) return;
        ClearSelection();
        _searching = true;
        _refresh.Disabled = true;
        _deadline = Time.GetTicksMsec() + 25000;
        _status.Text = "Finding available games...";
        try
        {
            if (!SteamworksHelper.Instance.SearchLobbies(_discovery, SearchCompleted))
                Fail("Previous search is still finishing. Try refreshing shortly.");
        }
        catch (Exception e) { Fail(e.Message); }
    }

    private void Fail(string reason)
    {
        SteamworksHelper.Instance.CancelLobbySearch(SearchCompleted);
        _searching = false;
        _refresh.Disabled = false;
        _status.Text = reason;
    }

    private void SearchCompleted(long[] lobbies)
    {
        if (!_searching || !IsInsideTree() || IsQueuedForDeletion()) return;
        _searching = false;
        _refresh.Disabled = false;
        try
        {
            var seen = new HashSet<long>();
            foreach (var lobby in lobbies)
            {
                if (lobby <= 0 || !seen.Add(lobby) ||
                    !_discovery.Matches(key => SteamworksHelper.Api.GetLobbyData(lobby, key))) continue;
                var state = SteamworksHelper.Api.GetLobbyData(lobby, "state");
                if (state != "ready" && state != "diagnostic") continue;
                var members = SteamworksHelper.Api.GetNumLobbyMembers(lobby);
                var capacity = SteamworksHelper.Api.GetLobbyMemberLimit(lobby);
                if (capacity <= 0 || members >= capacity) continue;

                var row = GD.Load<PackedScene>("res://scenes/ui/menu/start_menu/lobby_list_entry.tscn").Instantiate<Button>();
                row.GetNode<Label>("MarginContainer/MainRow/LobbyNameLabel").Text = SteamworksHelper.Api.GetLobbyData(lobby, "name");
                row.GetNode<Label>("MarginContainer/MainRow/HostNameLabel").Text = SteamworksHelper.Api.GetLobbyData(lobby, "mode");
                row.GetNode<Label>("MarginContainer/MainRow/PlayerCountLabel").Text = $"{members}/{capacity}";
                row.FocusMode = FocusModeEnum.All;
                row.ButtonGroup = _selection;
                row.Pressed += () =>
                {
                    SelectedLobbyId = row.ButtonPressed ? checked((ulong)lobby) : 0;
                    SelectionChanged?.Invoke(SelectedLobbyId);
                };
                _entries.AddChild(row);
            }
            _status.Text = _entries.GetChildCount() == 0
                ? "No matching games found. Try refreshing."
                : "Select a game, then Join Steam Game.";
        }
        catch (Exception e)
        {
            ClearSelection();
            Fail(e.Message);
        }
    }

    public override void _Process(double delta)
    {
        if (_searching && Time.GetTicksMsec() >= _deadline) Fail("Game search timed out. Try refreshing.");
    }

    public override void _ExitTree()
    {
        _searching = false;
        SteamworksHelper.Instance?.CancelLobbySearch(SearchCompleted);
        SelectionChanged = null;
    }
}
