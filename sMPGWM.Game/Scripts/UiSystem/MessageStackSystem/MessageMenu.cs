#nullable enable
using System;
using Godot;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.MessageStackSystem;

public partial class MessageMenu : GameOverlayMenu
{
    protected override string ScreenTitle => "Messages";
    private MessageStore? _store;
    private MessageViewState _state = new();
    private string? _requiredId;
    private VBoxContainer _senders = null!;
    private ScrollContainer _senderScroll = null!;
    private ScrollContainer _bodyScroll = null!;
    private MessageEntry _entry = null!;
    private Button _ok = null!;
    private bool _refreshQueued;
    private bool _hasRendered;
    private bool _scrollRestorePending;
    private bool _selectOnReady;
    private string? _displayedId;
    internal event Action? Acknowledged;

    internal void Bind(MessageStore? store, MessageViewState state)
    {
        Unbind();
        _hasRendered = false;
        _state = state;
        _store = store;
        if (store != null) store.Changed += OnChanged;
        if (IsNodeReady()) SelectFirstUnread();
    }

    internal void Unbind()
    {
        if (_store != null) _store.Changed -= OnChanged;
        if (IsNodeReady()) SaveScroll();
        _store = null;
        if (IsNodeReady()) Refresh();
    }

    protected override void OnReady()
    {
        base.OnReady();
        _senders = GetNode<VBoxContainer>("%SenderList");
        _senderScroll = GetNode<ScrollContainer>("%MessageList");
        _entry = GetNode<MessageEntry>("%MessageEntry");
        _bodyScroll = _entry.GetNode<ScrollContainer>("BodyScrollContainer");
        _ok = GetNode<Button>("%AcknowledgeButton");
        _ok.GuiInput += OnOkInput;
        ApplyRequired();
        if (_selectOnReady) SelectFirstUnread();
        else Refresh();
    }

    internal void SelectFirstUnread()
    {
        if (!IsNodeReady()) { _selectOnReady = true; return; }
        if (_requiredId != null) { Select(_requiredId, true); return; }
        var snapshot = _store?.Snapshot;
        PlayerMessage? selected = null;
        if (snapshot != null)
        {
            foreach (var message in snapshot.Messages)
                if (!message.IsRead) { selected = message; break; }
            selected ??= snapshot.Find(_state.SelectedId);
            if (selected == null)
                foreach (var message in snapshot.Messages)
                    if (selected == null || message.ReceivedAt > selected.ReceivedAt ||
                        message.ReceivedAt == selected.ReceivedAt && message.ArrivalSequence > selected.ArrivalSequence)
                        selected = message;
        }
        Select(selected?.Id, true);
    }

    internal void SetRequired(string? id)
    {
        _requiredId = id;
        if (!IsNodeReady()) { _selectOnReady = true; return; }
        ApplyRequired();
        if (id != null) Select(id, true);
    }

    private void ApplyRequired()
    {
        GetNode<Button>("%CloseButton").Visible = _requiredId == null;
        _ok.Visible = _requiredId != null;
    }

    private bool _okPressed;
    private void OnOkInput(InputEvent input)
    {
        if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse) return;
        if (mouse.Pressed) _okPressed = true;
        else if (_okPressed)
        {
            _okPressed = false;
            if (!new Rect2(Vector2.Zero, _ok.Size).HasPoint(mouse.Position)) return;
            _ok.AcceptEvent();
            Acknowledged?.Invoke();
        }
    }

    private void OnChanged(MessageSnapshot snapshot)
    {
        if (!IsNodeReady() || _refreshQueued) return;
        _refreshQueued = true;
        Callable.From(() => { _refreshQueued = false; if (GodotObject.IsInstanceValid(this) && IsInsideTree()) Refresh(); }).CallDeferred();
    }

    private void Select(string? id, bool reveal)
    {
        SaveScroll();
        if (_state.SelectedId != id) _state.BodyScroll = 0;
        _state.SelectedId = id;
        var message = _store?.Snapshot.Find(id);
        if (reveal && message != null) _state.CollapsedSenders.Remove(message.Sender.Id);
        Refresh();
        if (reveal && id != null)
            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
                foreach (var group in _senders.GetChildren())
                {
                    var list = group.GetNodeOrNull<VBoxContainer>("MessageList");
                    if (list == null) continue;
                    foreach (var row in list.GetChildren())
                        if (row.GetMeta("message_id").AsString() == id)
                            _senderScroll.EnsureControlVisible((Control)row);
                }
            }).CallDeferred();
    }

    private void SaveScroll()
    {
        if (_senderScroll == null || !_hasRendered || _scrollRestorePending) return;
        _state.SenderScroll = _senderScroll.ScrollVertical;
        _state.BodyScroll = _bodyScroll.ScrollVertical;
    }

    private void Refresh()
    {
        if (_senders == null) return;
        if (_displayedId == _state.SelectedId) SaveScroll();
        foreach (var child in _senders.GetChildren()) { _senders.RemoveChild(child); child.QueueFree(); }
        var snapshot = _store?.Snapshot;
        SenderGroup? group = null;
        string? senderId = null;
        if (snapshot != null)
            foreach (var message in snapshot.Messages)
            {
                if (senderId != message.Sender.Id)
                {
                    senderId = message.Sender.Id;
                    var stableId = senderId;
                    group = GameOverlayProvider.CreateSenderGroup();
                    _senders.AddChild(group);
                    group.Bind(message.Sender.DisplayName, snapshot.UnreadForSender(senderId),
                        !_state.CollapsedSenders.Contains(senderId), _requiredId != null,
                        expanded => { if (expanded) _state.CollapsedSenders.Remove(stableId); else _state.CollapsedSenders.Add(stableId); });
                }
                var row = GameOverlayProvider.CreateMessageListItem();
                group!.GetNode<VBoxContainer>("MessageList").AddChild(row);
                row.SetMeta("message_id", message.Id);
                row.Display(message, message.Id == _state.SelectedId);
                row.Disabled = _requiredId != null;
                row.Pressed += () => Select(message.Id, false);
            }
        if (snapshot == null || snapshot.Messages.Count == 0) _senders.AddChild(new Label { Text = "No messages" });
        var selected = snapshot?.Find(_state.SelectedId);
        _entry.Display(selected!);
        _displayedId = _state.SelectedId;
        _hasRendered = true;
        _scrollRestorePending = true;
        var expectedStore = _store;
        var expectedId = selected?.Id;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || _store != expectedStore || _state.SelectedId != expectedId) return;
            _senderScroll.ScrollVertical = _state.SenderScroll;
            _bodyScroll.ScrollVertical = _state.BodyScroll;
            _scrollRestorePending = false;
            if (expectedId != null) _store?.MarkRead(expectedId);
        }).CallDeferred();
    }

    public override void _ExitTree()
    {
        Unbind();
        _ok.GuiInput -= OnOkInput;
        base._ExitTree();
    }
}
