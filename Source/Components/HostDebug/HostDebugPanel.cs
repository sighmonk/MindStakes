using Godot;
using System;
using System.Linq;

/// <summary>
/// Host-only panel for live shows. Hidden by default; the host toggles it with the
/// <c>host_debug</c> input action (F10) to replace a bad question or switch the answering team
/// without restarting the round.
/// </summary>
public partial class HostDebugPanel : CanvasLayer
{
    private const string ToggleAction = "host_debug";

    [Export] public Control PanelContent { get; set; }
    [Export] public Label TitleLabel { get; set; }
    [Export] public Button RerollQuestionButton { get; set; }
    [Export] public Button NextTeamButton { get; set; }
    [Export] public Label StatusLabel { get; set; }
    [Export] public Button CloseButton { get; set; }

    public override void _Ready()
    {
        if (PanelContent == null)
        {
            GD.PushError($"{nameof(HostDebugPanel)} requires the {nameof(PanelContent)} export.");
            return;
        }

        PanelContent.Visible = false;

        if (RerollQuestionButton != null)
        {
            RerollQuestionButton.Pressed += OnRerollQuestionPressed;
        }

        if (NextTeamButton != null)
        {
            NextTeamButton.Pressed += OnNextTeamPressed;
        }

        if (CloseButton != null)
        {
            CloseButton.Pressed += HidePanel;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (PanelContent == null ||
            !InputMap.HasAction(ToggleAction) ||
            !@event.IsActionPressed(ToggleAction))
        {
            return;
        }

        if (PanelContent.Visible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }

        GetViewport().SetInputAsHandled();
    }

    private void ShowPanel()
    {
        TitleLabel.Text = Tr("HOST_DEBUG_TITLE");
        RerollQuestionButton.Text = Tr("HOST_DEBUG_REROLL_QUESTION");
        NextTeamButton.Text = Tr("HOST_DEBUG_NEXT_TEAM");
        CloseButton.Text = Tr("CLOSE");
        StatusLabel.Text = string.Empty;
        PanelContent.Visible = true;
    }

    private void HidePanel()
    {
        PanelContent.Visible = false;
    }

    private void OnRerollQuestionPressed()
    {
        if (!IsQuestionScreen())
        {
            StatusLabel.Text = Tr("HOST_DEBUG_ONLY_IN_QUESTION");
            return;
        }

        if (!QuestionsManager.Instance.RerollCurrentQuestion())
        {
            StatusLabel.Text = Tr("HOST_DEBUG_NO_REPLACEMENT");
            return;
        }

        HidePanel();
        ReloadCurrentScene();
    }

    private void OnNextTeamPressed()
    {
        var teams = TeamsManager.Instance.Teams;
        if (teams.Count == 0)
        {
            return;
        }

        var currentIndex = teams.IndexOf(TeamsManager.Instance.CurrentTeam);
        TeamsManager.Instance.SetAnsweringTeam((currentIndex + 1) % teams.Count);
        StatusLabel.Text = string.Format(Tr("HOST_DEBUG_ANSWERING"), TeamsManager.Instance.CurrentTeam.TeamName);
    }

    private bool IsQuestionScreen()
    {
        var scenePath = GetTree().CurrentScene?.SceneFilePath;
        return !string.IsNullOrEmpty(scenePath) &&
               GameModeRegistry.All.Any(mode => mode.ScenePath == scenePath);
    }

    private void ReloadCurrentScene()
    {
        var scenePath = GetTree().CurrentScene?.SceneFilePath;
        if (!string.IsNullOrEmpty(scenePath))
        {
            SceneManager.Instance.ChangeScene(scenePath);
        }
    }
}
