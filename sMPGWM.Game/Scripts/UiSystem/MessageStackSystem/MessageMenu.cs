#nullable enable
using System;
using System.Collections.Generic;
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
    private Label? _empty;
    private readonly Dictionary<string, SenderGroup> _groups = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MessageListItem> _rows = new(StringComparer.Ordinal);
    private bool _refreshQueued;
    private bool _deferredQueued;
    private int _bindingVersion;
    private bool _hasRendered;
    private bool _scrollRestorePending;
    private bool _selectOnReady;
    private string? _displayedId;
    private PlayerMessage? _displayedMessage;
    private string? _revealId;
    internal event Action? Acknowledged;

    internal void Bind(MessageStore? store, MessageViewState state)
    {
        Unbind();
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
        _bindingVersion++;
        _refreshQueued = false;
        _deferredQueued = false;
        _scrollRestorePending = false;
        _revealId = null;
        _okPressed = false;
        _hasRendered = false;
        _displayedId = null;
        _displayedMessage = null;
        if (!IsNodeReady()) return;
        foreach (var group in _groups.Values)
        {
            _senders.RemoveChild(group);
            group.QueueFree();
        }
        _groups.Clear();
        _rows.Clear();
        SetEmpty(true);
        _entry.Display(null!);
    }

    protected override void OnReady()
    {
        base.OnReady();
        _senders = GetNode<VBoxContainer>("%SenderList");
        _senderScroll = GetNode<ScrollContainer>("%MessageList");
        _entry = GetNode<MessageEntry>("%MessageEntry");
        _bodyScroll = _entry.GetNode<ScrollContainer>("BodyScrollContainer");
        _ok = GetNode<Button>("%AcknowledgeButton");
        SetEmpty(true);
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
        if (!IsNodeReady()) return;
        _refreshQueued = true;
        QueueDeferredUpdate();
    }

    private void Select(string? id, bool reveal)
    {
        SaveScroll();
        if (_state.SelectedId != id) _state.BodyScroll = 0;
        _state.SelectedId = id;
        var message = _store?.Snapshot.Find(id);
        if (reveal && message != null) _state.CollapsedSenders.Remove(message.Sender.Id);
        _revealId = reveal ? id : null;
        Refresh();
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
        _refreshQueued = false;
        if (_displayedId == _state.SelectedId) SaveScroll();
        var snapshot = _store?.Snapshot;
        VBoxContainer? list = null;
        string? senderId = null;
        var groupIndex = 0;
        var rowIndex = 0;
        // Messages are append-only within a binding; rebinding clears the cached controls.
        if (snapshot != null)
            foreach (var message in snapshot.Messages)
            {
                if (senderId != message.Sender.Id)
                {
                    senderId = message.Sender.Id;
                    var expanded = !_state.CollapsedSenders.Contains(senderId);
                    var unread = snapshot.UnreadForSender(senderId);
                    if (!_groups.TryGetValue(senderId, out var group))
                    {
                        var stableId = senderId;
                        group = GameOverlayProvider.CreateSenderGroup();
                        _groups.Add(senderId, group);
                        _senders.AddChild(group);
                        group.Bind(message.Sender.DisplayName, unread, expanded, _requiredId != null,
                            value =>
                            {
                                if (value) _state.CollapsedSenders.Remove(stableId);
                                else _state.CollapsedSenders.Add(stableId);
                            });
                    }
                    else group.SetPresentation(message.Sender.DisplayName, unread, expanded, _requiredId != null);
                    if (group.GetIndex() != groupIndex) _senders.MoveChild(group, groupIndex);
                    groupIndex++;
                    list = group.GetNode<VBoxContainer>("MessageList");
                    rowIndex = 0;
                }
                if (!_rows.TryGetValue(message.Id, out var row))
                {
                    var stableId = message.Id;
                    row = GameOverlayProvider.CreateMessageListItem();
                    _rows.Add(stableId, row);
                    list!.AddChild(row);
                    row.SetMeta("message_id", stableId);
                    row.Pressed += () => Select(stableId, false);
                }
                if (row.GetIndex() != rowIndex) list!.MoveChild(row, rowIndex);
                rowIndex++;
                row.Display(message, message.Id == _state.SelectedId);
                row.Disabled = _requiredId != null;
            }
        SetEmpty(snapshot == null || snapshot.Messages.Count == 0);
        var selected = snapshot?.Find(_state.SelectedId);
        if (!ReferenceEquals(_displayedMessage, selected)) _entry.Display(selected!);
        _displayedMessage = selected;
        _displayedId = _state.SelectedId;
        _hasRendered = true;
        _scrollRestorePending = true;
        QueueDeferredUpdate();
    }

    private void SetEmpty(bool empty)
    {
        if (empty)
        {
            if (_empty != null) return;
            _empty = new Label { Text = "No messages" };
            _senders.AddChild(_empty);
        }
        else if (_empty != null)
        {
            _senders.RemoveChild(_empty);
            _empty.QueueFree();
            _empty = null;
        }
    }

    private void QueueDeferredUpdate()
    {
        if (_deferredQueued) return;
        _deferredQueued = true;
        var version = _bindingVersion;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || version != _bindingVersion) return;
            _deferredQueued = false;
            if (_refreshQueued)
            {
                Refresh();
                return;
            }
            _senderScroll.ScrollVertical = _state.SenderScroll;
            _bodyScroll.ScrollVertical = _state.BodyScroll;
            _scrollRestorePending = false;
            if (_revealId != null && _revealId == _state.SelectedId && _rows.TryGetValue(_revealId, out var row))
                _senderScroll.EnsureControlVisible(row);
            _revealId = null;
            if (_state.SelectedId != null) _store?.MarkRead(_state.SelectedId);
        }).CallDeferred();
    }

    public override void _ExitTree()
    {
        Unbind();
        _ok.GuiInput -= OnOkInput;
        base._ExitTree();
    }
}
