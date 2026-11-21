using Godot;
using System;

public partial class GameModeCard : MarginContainer
{
    [Export] public Label GameModeLabel;
    [Export] public SpinBox RoundCountSpinBox;
    [Export] public Button StartButton;
    [Export] public GameManager.GameModeEnum GameModeType;
    private GameModeDefinition gameMode;
    public override void _Ready()
    {
        gameMode = GameModeRegistry.GetByIndex((int)GameModeType);
        RoundCountSpinBox.Value = GameManager.Instance.MaxRounds;
        RoundCountSpinBox.ValueChanged += OnRoundCountChanged;
        UpdateGameModeLabel();
        StartButton.Pressed += OnStartButtonPressed;
    }
    private void OnRoundCountChanged(double value)
    {
        GameManager.Instance.MaxRounds = (int)value;
    }
    private void UpdateGameModeLabel()
    {
        GameModeLabel.Text = Tr(gameMode is null ? GameModeRegistry.UnknownDisplayNameKey : gameMode.DisplayNameKey);
    }
    private void OnStartButtonPressed()
    {
        GameManager.Instance.CurrentGameModeIndex = (int)GameModeType;
        GameManager.Instance.RoundCount = 0;
        CategoriesManager.Instance.SetUpCategories();
    }
}
