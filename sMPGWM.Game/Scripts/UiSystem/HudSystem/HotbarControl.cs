#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;
using sMPGWM.Scripts.Util.UiComponents;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;


namespace sMPGWM.Scripts.UiSystem.HudSystem;

public partial class HotbarControl : PanelContainer
{
    private HBoxContainer _buttonContainer = null!;
    private bool _isProcessingInput;
    private bool _canEdit;
    private ItemHotbarState? _state;
    private ItemHotbarAdapter? _adapter;
    private ItemVisuals? _visuals;
    private IReadOnlyList<IconDefinition> _actions = Array.Empty<IconDefinition>();
    internal ClickableIcon[] Icons => _buttonContainer.GetChildren().OfType<ClickableIcon>().ToArray();
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

        for (var i = 0; i < iconDefinitions.Count; i++)
        {
            var slotIndex = i;
            var icon = BaseFactory.CreateClickableIcon(iconDefinitions[i]);

            icon.Pressed += () => SlotPressed?.Invoke(slotIndex);
            icon.Disabled = !_isProcessingInput && !_canEdit;

            _buttonContainer.AddChild(icon);
        }
    }

    private void ClearButtons()
    {
        var children = _buttonContainer.GetChildren();
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

    internal void Bind(ItemHotbarState state, ItemHotbarAdapter adapter, ItemCatalog catalog, IReadOnlyList<IconDefinition> actions)
    {
        Unbind();
        _state = state;
        _adapter = adapter;
        _visuals = new ItemVisuals(catalog);
        _actions = actions;
        SetIcons(actions);
        state.Changed += Refresh;
        Refresh();
    }

    internal void Unbind()
    {
        if (_state != null) _state.Changed -= Refresh;
        _state = null;
        _adapter = null;
        _visuals = null;
        _actions = Array.Empty<IconDefinition>();
        SetEditing(false);
    }

    internal void Refresh()
    {
        if (_state == null || _adapter == null) return;
        var icons = Icons;
        for (var position = 0; position < icons.Length; position++)
        {
            var icon = icons[position];
            icon.Binding = new HotbarCellBinding(position, _state.Revision);
            var availability = _adapter.Resolve(position);
            if (_state.Entries[position] is ActionHotbarBinding action && int.TryParse(action.ActionKey, out var originalIndex))
            {
                icon.SetDisplay(_actions[originalIndex].IconTexture, new IconDisplayData($"Skill {originalIndex + 1}"));
                icon.Modulate = Colors.White;
            }
            else _visuals!.ApplyDefinition(icon, availability.DefinitionId, availability.Quantity, availability.Label,
                availability.Available, _state.Entries[position] is WeaponHotbarBinding);
        }
    }

    public override void _ExitTree() => Unbind();
    
}

internal sealed record HotbarCellBinding(int Position, long Revision);
