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
    [Export] public Button ImportConfigButton { get; set; }
    [Export] public Button ExportConfigButton { get; set; }
    [Export] public FileDialog ImportConfigDialog { get; set; }
    [Export] public FileDialog ExportConfigDialog { get; set; }

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

        if (ImportConfigButton != null)
        {
            ImportConfigButton.Text = Tr("CONFIG_IMPORT");
            ImportConfigButton.Pressed += OnImportConfigPressed;
        }

        if (ExportConfigButton != null)
        {
            ExportConfigButton.Text = Tr("CONFIG_EXPORT");
            ExportConfigButton.Pressed += OnExportConfigPressed;
        }

        if (ImportConfigDialog != null)
        {
            ImportConfigDialog.FileSelected += OnImportConfigSelected;
        }

        if (ExportConfigDialog != null)
        {
            ExportConfigDialog.FileSelected += OnExportConfigSelected;
        }
    }

    private void OnImportConfigPressed()
    {
        ImportConfigDialog.Title = Tr("CONFIG_IMPORT");
        ImportConfigDialog.CurrentDir = ProjectSettings.GlobalizePath(ConfigArchive.ConfigsFolder);
        ImportConfigDialog.PopupCenteredRatio(0.8f);
    }

    private void OnExportConfigPressed()
    {
        ExportConfigDialog.Title = Tr("CONFIG_EXPORT");
        ExportConfigDialog.CurrentDir = ProjectSettings.GlobalizePath(ConfigArchive.ConfigsFolder);
        ExportConfigDialog.CurrentFile = "mindstakes-config" + ConfigArchive.ArchiveExtension;
        ExportConfigDialog.PopupCenteredRatio(0.8f);
    }

    private void OnImportConfigSelected(string path)
    {
        if (!ConfigArchive.Import(path))
        {
            ShowSaveError("CONFIG_IMPORT_FAILED");
        }
    }

    private void OnExportConfigSelected(string path)
    {
        if (!ConfigArchive.Export(path))
        {
            ShowSaveError("CONFIG_EXPORT_FAILED");
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
