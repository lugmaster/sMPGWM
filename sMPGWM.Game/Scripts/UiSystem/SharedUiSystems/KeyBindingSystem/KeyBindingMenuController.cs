using Godot;
using System.Collections.Generic;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.InputSystem;

namespace sMPGWM.Scripts.UiSystem.SharedUiSystems.KeyBindingSystem;

public partial class KeyBindingMenuController : VBoxContainer
{
    private InputEvent?[,] _draft = new InputEvent?[0, 2];
    private InputEvent?[,] _saved = new InputEvent?[0, 2];
    private Button[,] _buttons = new Button[0, 2];
    private Button _saveButton = null!;
    private Button _defaultsButton = null!;
    private Button _cancelCaptureButton = null!;
    private Button _clearBindingButton = null!;
    private HBoxContainer _captureControls = null!;
    private Label _status = null!;
    private int _captureAction = -1;
    private int _captureSlot;
    private InputEvent? _releaseToConsume;

    public override void _Ready()
    {
        _saved = KeyBindingSettings.CreateCurrentBindings();
        _draft = (InputEvent?[,])_saved.Clone();
        _buttons = new Button[KeyBindingCatalog.Actions.Length, 2];
        var menu = GetNode<VBoxContainer>("MenuContainer");
        var sections = menu.GetNode<VBoxContainer>("ScrollContainer/MarginContainer/SectionsContainer");
        RemoveLayoutSamples(sections);
        PopulateSections(sections);

        var actions = menu.GetNode<HBoxContainer>("ActionsContainer");
        _saveButton = actions.GetNode<Button>("ApplyButton");
        _saveButton.Text = "Save";
        _saveButton.Pressed += Save;
        _defaultsButton = actions.GetNode<Button>("ResetDefaultsButton");
        _defaultsButton.Text = "Restore Defaults";
        _defaultsButton.Pressed += RestoreDefaults;

        _status = new Label
        {
            Name = "BindingStatus",
            CustomMinimumSize = new Vector2(0, 44),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        menu.AddChild(_status);
        menu.MoveChild(_status, actions.GetIndex());
        _captureControls = new HBoxContainer { Name = "CaptureControls", Visible = false };
        _cancelCaptureButton = new Button { Name = "CancelCaptureButton", Text = "Cancel capture" };
        _clearBindingButton = new Button { Name = "ClearBindingButton", Text = "Clear binding" };
        _captureControls.AddChild(_cancelCaptureButton);
        _captureControls.AddChild(_clearBindingButton);
        menu.AddChild(_captureControls);
        menu.MoveChild(_captureControls, actions.GetIndex());
        _cancelCaptureButton.Pressed += CancelCapture;
        _clearBindingButton.Pressed += ClearBinding;
        VisibilityChanged += OnVisibilityChanged;
        Refresh();
        SetProcessInput(false);
    }

    private void PopulateSections(VBoxContainer sections)
    {
        var sectionScene = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/keybinding/keybinding_section.tscn");
        var rowScene = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/keybinding/keybinding_row.tscn");
        var rowsBySection = new Dictionary<string, VBoxContainer>();
        for (var actionIndex = 0; actionIndex < KeyBindingCatalog.Actions.Length; actionIndex++)
        {
            var definition = KeyBindingCatalog.Actions[actionIndex];
            if (!rowsBySection.TryGetValue(definition.Section, out var rows))
            {
                var section = sectionScene.Instantiate<VBoxContainer>();
                section.Name = $"Section{rowsBySection.Count}";
                section.GetNode<Label>("SectionHeader/SectionLabel").Text = definition.Section;
                rows = section.GetNode<VBoxContainer>("RowsContainer");
                RemoveLayoutSamples(rows);
                sections.AddChild(section);
                rowsBySection.Add(definition.Section, rows);
            }

            var row = rowScene.Instantiate<HBoxContainer>();
            row.Name = definition.Action;
            row.GetNode<Label>("ActionLabel").Text = definition.Label;
            rows.AddChild(row);
            BindButton(row.GetNode<Button>("PrimaryBindingButton"), actionIndex, 0);
            BindButton(row.GetNode<Button>("SecondaryBindingButton"), actionIndex, 1);
        }
    }

    private static void RemoveLayoutSamples(Node container)
    {
        // Keep the scene's editor examples; replace them only when this menu initializes.
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void BindButton(Button button, int actionIndex, int slot)
    {
        _buttons[actionIndex, slot] = button;
        button.Pressed += () => BeginCapture(actionIndex, slot);
    }

    private void BeginCapture(int actionIndex, int slot)
    {
        _captureAction = actionIndex;
        _captureSlot = slot;
        _releaseToConsume = null;
        SetProcessInput(true);
        Refresh();
    }

    public override void _Input(InputEvent input)
    {
        if (!IsVisibleInTree()) return;
        if (_releaseToConsume != null)
        {
            if (input is InputEventKey { Echo: true })
                GetViewport().SetInputAsHandled();
            if (IsReleaseOf(input, _releaseToConsume))
            {
                GetViewport().SetInputAsHandled();
                _releaseToConsume = null;
                SetProcessInput(_captureAction >= 0);
            }
            return;
        }
        if (_captureAction < 0) return;

        // These two explicit controls remain clickable while mouse buttons are being captured.
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } &&
            (_cancelCaptureButton.GetGlobalRect().HasPoint(GetGlobalMousePosition()) ||
             _clearBindingButton.GetGlobalRect().HasPoint(GetGlobalMousePosition()))) return;

        if (input is not InputEventKey && input is not InputEventMouseButton) return;
        GetViewport().SetInputAsHandled();
        var binding = KeyBindingSettings.Normalize(input);
        if (binding == null) return;
        _draft[_captureAction, _captureSlot] = binding;
        _releaseToConsume = binding is InputEventMouseButton
            { ButtonIndex: >= MouseButton.WheelUp and <= MouseButton.WheelRight } ? null : binding;
        _captureAction = -1;
        Refresh();
        SetProcessInput(_releaseToConsume != null);
    }

    private static bool IsReleaseOf(InputEvent input, InputEvent binding)
    {
        return (input, binding) switch
        {
            (InputEventKey { Pressed: false } key, InputEventKey bound) =>
                bound.PhysicalKeycode != Key.None ? key.PhysicalKeycode == bound.PhysicalKeycode : key.Keycode == bound.Keycode,
            (InputEventMouseButton { Pressed: false } mouse, InputEventMouseButton bound) =>
                mouse.ButtonIndex == bound.ButtonIndex,
            _ => false
        };
    }

    private void CancelCapture()
    {
        _captureAction = -1;
        _releaseToConsume = null;
        SetProcessInput(false);
        Refresh();
    }

    private void ClearBinding()
    {
        if (_captureAction < 0) return;
        _draft[_captureAction, _captureSlot] = null;
        CancelCapture();
    }

    private void OnVisibilityChanged()
    {
        if (!IsVisibleInTree() && _status != null) CancelCapture();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && _status != null) CancelCapture();
    }

    private void RestoreDefaults()
    {
        _draft = KeyBindingSettings.CreateDefaultBindings();
        CancelCapture();
    }

    private void Save()
    {
        if (_captureAction >= 0 || FindConflict() != null) return;
        var error = GameConfig.SaveKeyBindings(_draft);
        if (error != Error.Ok)
        {
            _status.Text = $"Could not save keybindings ({error}). Your changes are still available to retry.";
            return;
        }
        _saved = (InputEvent?[,])_draft.Clone();
        Refresh();
        _status.Text = "Keybindings saved.";
    }

    private void Refresh()
    {
        var dirty = false;
        for (var action = 0; action < _draft.GetLength(0); action++)
        for (var slot = 0; slot < 2; slot++)
        {
            var button = _buttons[action, slot];
            button.Text = _captureAction == action && _captureSlot == slot
                ? "Press input…" : KeyBindingSettings.Format(_draft[action, slot]);
            button.Disabled = _captureAction >= 0;
            button.TooltipText = $"{KeyBindingCatalog.Actions[action].Label}: {button.Text}. Click to change or clear.";
            dirty |= !KeyBindingSettings.SameBinding(_draft[action, slot], _saved[action, slot]);
        }

        var conflict = FindConflict();
        _saveButton.Disabled = !dirty || _captureAction >= 0 || conflict != null;
        _defaultsButton.Disabled = _captureAction >= 0;
        _captureControls.Visible = _captureAction >= 0;
        _status.Text = _captureAction >= 0
            ? $"{KeyBindingCatalog.Actions[_captureAction].Label}: press a key or mouse button (modifiers supported)."
            : conflict ?? (dirty ? "Unsaved changes. Save to apply; Back discards changes." : "Select a binding to change or clear it.");
    }

    private string? FindConflict()
    {
        for (var action = 0; action < _draft.GetLength(0); action++)
        for (var slot = 0; slot < 2; slot++)
        {
            var binding = _draft[action, slot];
            if (binding == null) continue;
            for (var other = action; other < _draft.GetLength(0); other++)
            for (var otherSlot = other == action ? slot + 1 : 0; otherSlot < 2; otherSlot++)
                if (KeyBindingSettings.SameBinding(binding, _draft[other, otherSlot]))
                    return $"{KeyBindingSettings.Format(binding)} is assigned to both {KeyBindingCatalog.Actions[action].Label} and {KeyBindingCatalog.Actions[other].Label}. Change or clear one before saving.";
        }
        return null;
    }
}
