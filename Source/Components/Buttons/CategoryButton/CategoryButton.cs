using Godot;
using System;
using System.Collections.Generic;

public partial class CategoryButton : Button
{
    public Category Category { get; set; }
    [Export(PropertyHint.File, "*.tscn")]
    public string QuestionScenePath { get; set; }
    [Export(PropertyHint.File, "*.tscn")]
    public string BettingScenePath { get; set; }
    [Export(PropertyHint.File, "*.tscn")]
    public string SabotageScenePath { get; set; }
    [Export(PropertyHint.File, "*.tscn")]
    public string FraudScenePath { get; set; }
    private readonly Dictionary<string, string> scenePathOverrides = new();
    public override void _Ready()
    {
        CacheScenePathOverrides();
        Pressed += OnPressed;
        UpdateButton();
    }
    private void CacheScenePathOverrides()
    {
        scenePathOverrides[GameModeRegistry.BettingId] = BettingScenePath;
        scenePathOverrides[GameModeRegistry.SabotageId] = SabotageScenePath;
        scenePathOverrides[GameModeRegistry.FraudId] = FraudScenePath;
    }
    public void UpdateButton()
    {
        if (Category != null)
        {
            Text = Category.CategoryName;
        }
    }
    public void OnPressed()
    {
        CategoriesManager.Instance.RandomCategory = Category;
        QuestionsManager.Instance.SetRandomQuestion();
        ChangeScene();
    }

    public void ChangeScene()
    {
        var gameMode = GameManager.Instance.CurrentGameMode;
        if (gameMode is null)
        {
            GD.PrintErr($"Unknown Game Mode Index {GameManager.Instance.CurrentGameModeIndex}");
            return;
        }

        var scenePath = GetScenePath(gameMode);
        if (string.IsNullOrEmpty(scenePath))
        {
            GD.PrintErr($"No scene configured for game mode '{gameMode.Id}'");
            return;
        }

        SceneManager.Instance.ReplaceWithNewScene(scenePath);
    }

    private string GetScenePath(GameModeDefinition gameMode)
    {
        if (scenePathOverrides.TryGetValue(gameMode.Id, out var scenePathOverride) &&
            !string.IsNullOrEmpty(scenePathOverride))
        {
            return scenePathOverride;
        }

        return gameMode.ScenePath;
    }
}
