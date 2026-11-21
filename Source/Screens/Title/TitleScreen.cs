using Godot;
using System;

public partial class TitleScreen : Control
{
    [Export] public Label TitleLabel { get; set; }
    [Export] public CheckBox AnimationsCheckBox { get; set; }

    public override void _Ready()
    {
        GameManager.Instance.StartNewSession();

        if (AnimationsCheckBox != null)
        {
            AnimationsCheckBox.Text = Tr("SETTINGS_ANIMATIONS");
            AnimationsCheckBox.ButtonPressed = UiSettings.AnimationsEnabled;
            AnimationsCheckBox.Toggled += OnAnimationsToggled;
        }
    }

    private void OnAnimationsToggled(bool enabled)
    {
        UiSettings.AnimationsEnabled = enabled;
    }
}
