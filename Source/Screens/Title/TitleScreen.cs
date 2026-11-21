using Godot;
using System;

public partial class TitleScreen : Control
{
    [Export] public Label TitleLabel { get; set; }
    [Export] public Button AnimationsButton { get; set; }

    public override void _Ready()
    {
        GameManager.Instance.StartNewSession();

        if (AnimationsButton != null)
        {
            AnimationsButton.Toggled += OnAnimationsToggled;
            UpdateAnimationsButton();
        }
    }

    private void OnAnimationsToggled(bool enabled)
    {
        UiSettings.AnimationsEnabled = enabled;
        UpdateAnimationsButton();
    }

    private void UpdateAnimationsButton()
    {
        var enabled = UiSettings.AnimationsEnabled;
        AnimationsButton.SetPressedNoSignal(enabled);
        AnimationsButton.Text = Tr(enabled ? "SETTINGS_ANIMATIONS_ON" : "SETTINGS_ANIMATIONS_OFF");
    }
}
