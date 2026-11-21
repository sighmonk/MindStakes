using Godot;
using System;

public partial class BettingScreen : Control
{
    
    [Export] public PackedScene BettingTeamCardScene;
    [Export] public HBoxContainer TeamCardsContainer;
    [Export] public Label CategoryLabel;
    public override void _Ready()
    {
        LoadTeamCards();
        UpdateCategoryLabel();
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
    public void LoadTeamCards()
    {
        for (int i = 0; i < TeamsManager.Instance.Teams.Count; i++)
        {
            AddTeamCard(TeamsManager.Instance.Teams[i]);
        }
    }

    public void AddTeamCard(Team newTeam)
    {
        BettingTeamCard teamCard = BettingTeamCardScene.Instantiate<BettingTeamCard>();
        teamCard.TeamData = newTeam;
        TeamCardsContainer.AddChild(teamCard);
    }
}
