 using Godot;
using System;
using System.Linq;

public partial class ScoreScreen : Control
{
    [Export] public PackedScene ScoreTeamCardScene;
    [Export] public HBoxContainer TeamCardsContainer;
    [Export] public NextButton ReadyButton;
    [Export] public Label CategoryLabel;
    public override void _Ready()
    {
        LoadTeamCards();
        UpdateCategoryLabel();
        CheckGameEnd();
        SignalManager.Instance.AnsweringTeamChanged += OnAnsweringTeamChanged;
    }

    private void UpdateCategoryLabel()
    {
        if (CategoryLabel == null)
        {
            return;
        }

        var categoryName = CategoriesManager.Instance.RandomCategory?.CategoryName;
        CategoryLabel.Text = string.IsNullOrEmpty(categoryName)
            ? string.Empty
            : Tr("CATEGORY_PREFIX") + categoryName;
    }

    private void OnAnsweringTeamChanged()
    {
        foreach (var teamCard in TeamCardsContainer.GetChildren().OfType<ScoreTeamCard>())
        {
            teamCard.UpdateButtonsAndText();
        }
    }

    private void CheckGameEnd()
    {
        if (GameManager.Instance.RoundCount + 1 >= GameManager.Instance.MaxRounds)
        {
            if (ReadyButton != null)
            {
                ReadyButton.ScenePath = "";
            }
        }
    }

    public void LoadTeamCards()
    {
        for (int i = 0; i < TeamsManager.Instance.Teams.Count; i++)
        {
            AddTeamCard(TeamsManager.Instance.Teams[i]);
        }
    }

    public void AddTeamCard(Team newTeam)
    {
        ScoreTeamCard teamCard = ScoreTeamCardScene.Instantiate<ScoreTeamCard>();
        teamCard.TeamData = newTeam;
        TeamCardsContainer.AddChild(teamCard);
        teamCard.UpdateButtonsAndText();
    }

    private void OnReadyButtonPressed()
    {
        QuestionsManager.Instance.RemoveCurrentQuestion();
        GameManager.Instance.RerollCount = GameManager.Instance.InitialRerollCount;
        GameManager.Instance.NextRound();
        TeamsManager.Instance.NextInitialTeam();
        TeamsManager.Instance.ResetBets();
        CategoriesManager.Instance.SetRandomCategory();
        QuestionsManager.Instance.SetRandomQuestion();
        SaveManager.Instance.SaveCurrentGame();
    }
}
