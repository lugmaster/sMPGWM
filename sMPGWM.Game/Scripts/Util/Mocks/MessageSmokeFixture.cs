#nullable enable
using System;
using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.MessageStackSystem;

namespace sMPGWM.Scripts.Util.Mocks;

public partial class MessageSmokeFixture : RefCounted
{
    private GamePlayManager _game = null!;
    private MessageStore? _empty;
    public void Bind(GamePlayManager game) => _game = game;
    public int Count() => _game.Messages.Snapshot.Messages.Count;
    public int Unread() => _game.Messages.Snapshot.UnreadCount;
    public bool Read(string id) => _game.Messages.Snapshot.Find(id)?.IsRead == true;
    public string Selected() => _game.GetNode<UiCanvas>("UiCanvas").GetNodeOrNull<MessageMenu>(
        "Root/MenuLayer/UiContainer/MenuHost/WindowScroll/WindowRow/MessageMenu")?.GetNode<Label>("%MessageEntry/Heading").Text ?? "";
    public bool Receive(string id, bool blocking = false)
    {
        return _game.Messages.Receive(id, "system", id, "Body: " + id,
            blocking ? MessageType.Blocking : MessageType.NonBlocking);
    }
    public void Populate()
    {
        var store = _game.Messages;
        store.RegisterSender(new MessageSender("station", "Station Command"));
        store.RegisterSender(new MessageSender("trading", "Trading Guild"));
        store.RegisterSender(new MessageSender("security", "Security Office"));
        store.RegisterSender(new MessageSender("other-security", "Security Office"));
        store.Receive("mission", "station", "Welcome to the sector", "Patrol the nearby sector. This message is informational.", MessageType.NonBlocking);
        store.Receive("trade", "trading", "Market update", "New supplies have arrived at the station.", MessageType.NonBlocking);
        store.Receive("security", "security", "Security advisory", "Keep an eye out for unidentified ships.", MessageType.NonBlocking);
        store.Receive("same-name", "other-security", "Independent office", "Sender identities remain separate.", MessageType.NonBlocking);
        var body = "";
        for (var i = 0; i < 150; i++) body += $"Sector report {i}: this long plain text wraps inside the detail pane.\n";
        store.Receive("long", "station", new string('W', 160), body, MessageType.NonBlocking);
    }
    public void PrepareReadHistory()
    {
        var snapshot = _game.Messages.Snapshot;
        foreach (var message in snapshot.Messages) _game.Messages.MarkRead(message.Id);
    }

    public void LargeCount()
    {
        for (var i = 0; i < 1050; i++) Receive("bulk" + i);
    }
    public void BindEmpty()
    {
        var empty = new MessageStore();
        try { _game.GetNode<UiCanvas>("UiCanvas").BindMessages(empty); }
        catch { empty.Dispose(); throw; }
        _empty?.Dispose();
        _empty = empty;
    }
    public bool TryBindEmpty()
    {
        try { BindEmpty(); return true; }
        catch (InvalidOperationException) { return false; }
    }
    public bool TryUnbindMessages()
    {
        try { _game.GetNode<UiCanvas>("UiCanvas").UnbindMessages(); return true; }
        catch (InvalidOperationException) { return false; }
    }
    public void Restore()
    {
        _game.GetNode<UiCanvas>("UiCanvas").BindMessages(_game.Messages);
        _empty?.Dispose();
        _empty = null;
    }
}
