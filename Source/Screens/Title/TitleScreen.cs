using Godot;
using System;

public partial class TitleScreen : Control
{
    [Export] public Label TitleLabel { get; set; }

    public override void _Ready()
    {
        GameManager.Instance.StartNewSession();
    }
}
