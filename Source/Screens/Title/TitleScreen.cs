using Godot;
using System;

public partial class TitleScreen : Control
{
    [Export] public Label TitleLabel { get; set; }
    [Export] public Button AnimationsButton { get; set; }
    [Export] public Button LoadSaveButton { get; set; }
    [Export] public Button ExportSaveButton { get; set; }
    [Export] public FileDialog LoadSaveDialog { get; set; }
    [Export] public FileDialog ExportSaveDialog { get; set; }
    [Export] public AcceptDialog SaveErrorDialog { get; set; }

    public override void _Ready()
    {
        GameManager.Instance.StartNewSession();

        if (AnimationsButton != null)
        {
            AnimationsButton.Toggled += OnAnimationsToggled;
            UpdateAnimationsButton();
        }

        if (LoadSaveButton != null)
        {
            LoadSaveButton.Text = Tr("SAVE_LOAD");
            LoadSaveButton.Pressed += OnLoadSavePressed;
        }

        if (ExportSaveButton != null)
        {
            ExportSaveButton.Text = Tr("SAVE_EXPORT");
            ExportSaveButton.Pressed += OnExportSavePressed;
        }

        if (LoadSaveDialog != null)
        {
            LoadSaveDialog.FileSelected += OnLoadSaveSelected;
        }

        if (ExportSaveDialog != null)
        {
            ExportSaveDialog.FileSelected += OnExportSaveSelected;
        }
    }

    private void OnLoadSavePressed()
    {
        LoadSaveDialog.Title = Tr("SAVE_LOAD");
        LoadSaveDialog.CurrentDir = ProjectSettings.GlobalizePath(SaveManager.SavesFolder);
        LoadSaveDialog.PopupCenteredRatio(0.8f);
    }

    private void OnExportSavePressed()
    {
        ExportSaveDialog.Title = Tr("SAVE_EXPORT");
        ExportSaveDialog.CurrentDir = ProjectSettings.GlobalizePath(SaveManager.SavesFolder);
        ExportSaveDialog.CurrentFile = "mindstakes-save" + SaveManager.SaveExtension;
        ExportSaveDialog.PopupCenteredRatio(0.8f);
    }

    private void OnLoadSaveSelected(string path)
    {
        if (!SaveManager.Instance.LoadGame(path))
        {
            ShowSaveError("SAVE_LOAD_FAILED");
        }
    }

    private void OnExportSaveSelected(string path)
    {
        if (!SaveManager.Instance.ExportCurrentGame(path))
        {
            ShowSaveError("SAVE_EXPORT_FAILED");
        }
    }

    private void ShowSaveError(string key)
    {
        if (SaveErrorDialog == null)
        {
            return;
        }

        SaveErrorDialog.DialogText = Tr(key);
        SaveErrorDialog.PopupCentered();
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
