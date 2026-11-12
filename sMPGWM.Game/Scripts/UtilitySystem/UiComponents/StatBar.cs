using System;
using Godot;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.UtilitySystem.UiComponents;

public partial class StatBar : Control
{
    // public StatType Type { get; private set; }

    private ProgressBar _progressBar = null!;
    private Label _nameLabel = null!;
    private Label _valueLabel = null!;

    public override void _EnterTree()
    {
        Logger.Trace($"{nameof(StatBar)}.{nameof(_EnterTree)} called.");
        _progressBar = GetNode<ProgressBar>("%ProgressBar");
        _nameLabel = GetNode<Label>("%NameLabel");
        _valueLabel = GetNode<Label>("%ValueLabel");
    }

    public override void _Ready()
    {
        Logger.Trace($"{nameof(StatBar)}.{nameof(_Ready)} called.");
    }

    // public void Configure(StatType type, string title)
    // {
    //     Type = type;
    //     Title = title;
    // }

    public string Title
    {
        get => _nameLabel.Text;
        private set => _nameLabel.Text = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void SetTitle(string title) => Title = title;

    public void Setup(double current, double maximum)
    {
        if (!double.IsFinite(maximum) || !double.IsFinite(current) || maximum < 0 || current < 0 || current > maximum)
        {
            Logger.Error($"{nameof(StatBar)}.{nameof(Setup)} received invalid {nameof(maximum)}={maximum}.");
            throw new ArgumentOutOfRangeException(nameof(maximum), "Resource values must be finite and within capacity.");
        }

        _progressBar.MinValue = 0;
        _progressBar.MaxValue = maximum;
        _progressBar.Value = current;
        _progressBar.ShowPercentage = false;

        _valueLabel.Text = $"{current:0}/{maximum:0}";
    }
}
