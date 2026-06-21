using System;
using Godot;
using sMPGWM.Scripts.Enums.Game;

namespace sMPGWM.Scripts.Ui.Base;

public partial class StatBar : Control
{
    public StatTypes Type { get; private set; }

    private ProgressBar _progressBar = null!;
    private Label _nameLabel = null!;
    private Label _valueLabel = null!;

    public override void _Ready()
    {
        _progressBar = GetNode<ProgressBar>("%ProgressBar");
        _nameLabel = GetNode<Label>("%NameLabel");
        _valueLabel = GetNode<Label>("%ValueLabel");
    }

    public void Configure(StatTypes type, string title)
    {
        Type = type;
        Title = title;
    }

    public string Title
    {
        get => _nameLabel.Text;
        private set => _nameLabel.Text = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void Setup(double current, double maximum)
    {
        if (maximum <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum must be greater than zero.");

        _progressBar.MinValue = 0;
        _progressBar.MaxValue = maximum;
        _progressBar.Value = current;
        _progressBar.ShowPercentage = false;

        _valueLabel.Text = $"{current:0}/{maximum:0}";
    }
}