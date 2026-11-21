using Godot;
using System;

public partial class CategoryPickScreen : Control
{
    private const string SpinAnimation = "spin";

    [Export] MarginContainer TeamCardContainer;
    [Export] PackedScene TeamCardScene;
    [Export] Panel CategoryButtonContainer;
    [Export] PackedScene CategoryButtonScene;
    [Export] Button RerollButton;
    [Export] public AnimationPlayer RerollAnimation { get; set; }
    [Export] public Control ReelArea { get; set; }
    [Export] public VBoxContainer ReelStrip { get; set; }
    public override void _Ready()
    {
        LoadTeamCard();
        if (CategoryButtonContainer != null)
        {
            CategoryButton categoryButton = CategoryButtonScene.Instantiate<CategoryButton>();
            categoryButton.Category = CategoriesManager.Instance.RandomCategory;
            CategoryButtonContainer.AddChild(categoryButton, true);
        }
        if (RerollButton != null)
        {
            RerollButton.Text = LocalizationManager.Format("REROLL_BUTTON", GameManager.Instance.RerollCount);
        }

        SignalManager.Instance.AnsweringTeamChanged += LoadTeamCard;
    }

    private void LoadTeamCard()
    {
        if (TeamCardContainer == null)
        {
            return;
        }

        foreach (var child in TeamCardContainer.GetChildren())
        {
            TeamCardContainer.RemoveChild(child);
            child.QueueFree();
        }

        TeamCard teamCard = TeamCardScene.Instantiate<TeamCard>();
        teamCard.TeamData = TeamsManager.Instance.InitialTeam;
        TeamCardContainer.AddChild(teamCard);
        teamCard.TeamNameLineEdit.Editable = false;
    }

    public override void _Process(double delta)
    {
        if (RerollButton != null)
        {
            RerollButton.Disabled = GameManager.Instance.RerollCount <= 0;
        }
    }

    public void OnRerollButtonPressed()
    {
        if (GameManager.Instance.RerollCount <= 0)
            return;

        CategoriesManager.Instance.SetRandomCategory();
        GameManager.Instance.RerollCount--;

        if (CanPlayRerollAnimation())
            PlayRerollAnimation();
        else
            RefreshCategoryButton();

        if (RerollButton != null)
        {
            RerollButton.Text = LocalizationManager.Format("REROLL_BUTTON", GameManager.Instance.RerollCount);
        }
    }

    private bool CanPlayRerollAnimation()
    {
        return RerollAnimation != null
            && ReelArea != null
            && ReelStrip != null
            && UiSettings.AnimationsEnabled;
    }

    private void PlayRerollAnimation()
    {
        FillReelSlots();
        HideCategoryButton();
        ReelArea.Visible = true;
        RerollAnimation.Play(SpinAnimation);
    }

    private void FillReelSlots()
    {
        var slots = ReelStrip.GetChildren();
        var categories = CategoriesManager.Instance.Categories;
        var finalCategory = CategoriesManager.Instance.RandomCategory;

        for (var i = 0; i < slots.Count - 1 && categories.Count > 0; i++)
        {
            var randomCategory = categories[(int)(GD.Randi() % (uint)categories.Count)];
            if (slots[i] is Label slot)
            {
                slot.Text = randomCategory.CategoryName;
            }
        }

        if (slots.Count > 0 && slots[^1] is Label lastSlot)
        {
            lastSlot.Text = finalCategory != null ? finalCategory.CategoryName : string.Empty;
        }
    }

    public void OnRerollAnimationFinished(StringName animationName)
    {
        if (animationName != SpinAnimation || ReelArea == null)
        {
            return;
        }

        ReelArea.Visible = false;
        RefreshCategoryButton();
    }

    private CategoryButton GetCategoryButton()
    {
        if (CategoryButtonContainer == null)
        {
            return null;
        }

        foreach (var child in CategoryButtonContainer.GetChildren())
        {
            if (child is CategoryButton button)
            {
                return button;
            }
        }

        return null;
    }

    private void HideCategoryButton()
    {
        var categoryButton = GetCategoryButton();
        if (categoryButton != null)
        {
            categoryButton.Visible = false;
        }
    }

    private void RefreshCategoryButton()
    {
        if (CategoryButtonContainer != null)
        {
            var currentButton = GetCategoryButton();
            if (currentButton != null)
            {
                CategoryButtonContainer.RemoveChild(currentButton);
                currentButton.QueueFree();
            }

            CategoryButton categoryButton = CategoryButtonScene.Instantiate<CategoryButton>();
            categoryButton.Category = CategoriesManager.Instance.RandomCategory;
            CategoryButtonContainer.AddChild(categoryButton, true);
            categoryButton.UpdateButton();
        }
    }
}
