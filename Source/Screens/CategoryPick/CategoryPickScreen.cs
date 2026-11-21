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

        var previousCategoryName = CategoriesManager.Instance.RandomCategory?.CategoryName;
        CategoriesManager.Instance.SetRandomCategory();
        GameManager.Instance.RerollCount--;

        if (CanPlayRerollAnimation())
            PlayRerollAnimation(previousCategoryName);
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

    private void PlayRerollAnimation(string previousCategoryName)
    {
        FillReelSlots(previousCategoryName);
        HideCategoryButton();
        ReelArea.Visible = true;
        RerollAnimation.Play(SpinAnimation);
    }

    private void FillReelSlots(string previousCategoryName)
    {
        var slots = ReelStrip.GetChildren();
        if (slots.Count < 3)
        {
            return;
        }

        var categories = CategoriesManager.Instance.Categories;
        var finalName = CategoriesManager.Instance.RandomCategory?.CategoryName ?? string.Empty;
        var previousName = string.IsNullOrEmpty(previousCategoryName) ? finalName : previousCategoryName;

        if (slots[0] is Label finalSlot)
        {
            finalSlot.Text = finalName;
        }

        var avoidName = finalName;
        for (var i = 1; i < slots.Count - 2; i++)
        {
            if (slots[i] is Label slot)
            {
                slot.Text = PickReelName(categories, finalName, avoidName);
                avoidName = slot.Text;
            }
        }

        if (slots[^2] is Label currentSlot)
        {
            currentSlot.Text = previousName;
        }

        if (slots[^1] is Label paddingSlot)
        {
            paddingSlot.Text = PickReelName(categories, finalName, previousName);
        }
    }

    private static string PickReelName(System.Collections.Generic.IReadOnlyList<Category> categories, string finalName, string avoidName)
    {
        if (categories.Count == 0)
        {
            return string.Empty;
        }

        for (var attempt = 0; attempt < 12; attempt++)
        {
            var candidate = categories[(int)(GD.Randi() % (uint)categories.Count)].CategoryName;
            if (candidate != finalName && candidate != avoidName)
            {
                return candidate;
            }
        }

        return string.Empty;
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
