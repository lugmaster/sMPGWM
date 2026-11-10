using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Items.Hotbar;
using sMPGWM.Scripts.UiSystem.Items;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.Util.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;


namespace sMPGWM.Scripts.UiSystem.HudSystem.Hotbar;

public partial class HotbarControl : PanelContainer
{
    private HBoxContainer _buttonContainer = null!;
    private bool _isProcessingInput;
    private bool _canEdit;
    private ItemHotbarController? _controller;
    private ItemVisuals? _visuals;
    private readonly Dictionary<string, IconDefinition> _actions = new(StringComparer.Ordinal);
    private ClickableIcon[] _icons = Array.Empty<ClickableIcon>();
    internal ClickableIcon[] Icons => _icons;
    internal event Action<int>? SlotPressed;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(HotbarControl)}.{nameof(_EnterTree)} called.");
        _buttonContainer = GetNode<HBoxContainer>("%ButtonContainer");
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(HotbarControl)}.{nameof(_Ready)} called.");
        ClearButtons();

        Logger.Debug($"{nameof(HotbarControl)}.{nameof(_Ready)} loaded.");
    }

    internal void SetIcons(IReadOnlyList<IconDefinition> iconDefinitions)
    {
        Logger.Debug($"{nameof(HotbarControl)}.{nameof(SetIcons)} called with {nameof(iconDefinitions)} count={iconDefinitions.Count}.");
        ClearButtons();

        _icons = new ClickableIcon[_controller?.Entries.Count ?? iconDefinitions.Count];
        for (var i = 0; i < _icons.Length; i++)
        {
            var slotIndex = i;
            var icon = i < iconDefinitions.Count
                ? BaseFactory.CreateClickableIcon(iconDefinitions[i])
                : HudProvider.CreateClickableIcon();
            icon.IconSize = IconSize.Large;

            icon.Pressed += () => SlotPressed?.Invoke(slotIndex);
            icon.Disabled = !_isProcessingInput && !_canEdit;

            _icons[i] = icon;
            _buttonContainer.AddChild(icon);
        }
    }

    private void ClearButtons()
    {
        var children = _buttonContainer.GetChildren();
        _icons = Array.Empty<ClickableIcon>();
        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is ClickableIcon child)
            {
                _buttonContainer.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
    
    internal void SetGameplayInputEnabled(bool enabled)
    {
        _isProcessingInput = enabled;
        var children = _buttonContainer.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is ClickableIcon child)
                child.Disabled = !_isProcessingInput && !_canEdit;
        }

    }

    internal void SetEditing(bool enabled)
    {
        _canEdit = enabled;
        SetGameplayInputEnabled(_isProcessingInput);
    }

    internal void Bind(ItemHotbarController controller, IReadOnlyList<IconDefinition> actions)
    {
        var keyedActions = new Dictionary<string, IconDefinition>(StringComparer.Ordinal);
        for (var i = 0; i < actions.Count; i++)
            keyedActions.Add(i.ToString(System.Globalization.CultureInfo.InvariantCulture), actions[i]);
        Bind(controller, keyedActions);
    }

    internal void Bind(ItemHotbarController controller, IReadOnlyDictionary<string, IconDefinition> actions)
    {
        Unbind();
        _controller = controller;
        _visuals = new ItemVisuals();
        foreach (var action in actions) _actions.Add(action.Key, action.Value);
        SetIcons(Array.Empty<IconDefinition>());
        controller.Changed += Refresh;
        Refresh();
    }

    internal void Unbind()
    {
        if (_controller != null) _controller.Changed -= Refresh;
        _controller = null;
        _visuals = null;
        _actions.Clear();
        SetEditing(false);
    }

    internal void Refresh()
    {
        if (_controller == null) return;
        var icons = Icons;
        for (var position = 0; position < icons.Length; position++)
        {
            var icon = icons[position];
            icon.Binding = ItemCellBinding.Hotbar(position, _controller.Revision);
            var availability = _controller.Resolve(position);
            if (_controller.Entries[position] is ActionHotbarBinding action)
            {
                _actions.TryGetValue(action.ActionKey, out var definition);
                var label = availability.Label == action.ActionKey && int.TryParse(action.ActionKey, out var originalIndex)
                    ? $"Skill {originalIndex + 1}" : availability.Label;
                icon.SetDisplay(definition?.IconTexture, new IconDisplayData(label));
                icon.DisplayModulate = Colors.White;
                icon.Modulate = Colors.White;
            }
            else _visuals!.ApplyDefinition(icon, availability.DefinitionId, availability.Quantity, availability.Label,
                availability.Available, _controller.Entries[position] is WeaponHotbarBinding);
        }
    }

    public override void _ExitTree() => Unbind();
    
}
