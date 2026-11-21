using Godot;
using System;
using System.Linq;

public partial class GameModePickScreen : Control
{
	[Export] public SpinBox RerollCountSpinBox { get; set; }
	[Export] public SpinBox PoolNumberSpinBox { get; set; }
	[Export] public SpinBox QuestionTimeSpinBox { get; set; }
	public override void _Ready()
	{
		RerollCountSpinBox.Value = GameManager.Instance.InitialRerollCount;
		QuestionTimeSpinBox.Value = GameManager.Instance.QuestionTime;
		
		// Setup PoolNumberSpinBox
		var maxPoolNumber = QuestionsManager.Instance.GetMaxPoolNumber();
		PoolNumberSpinBox.MinValue = 1;
		PoolNumberSpinBox.MaxValue = maxPoolNumber;
		PoolNumberSpinBox.Value = GameManager.Instance.SelectedPoolNumber;
		
		// Load questions from the selected pool
		QuestionsManager.Instance.LoadQuestionsFromPool(GameManager.Instance.SelectedPoolNumber);
		
		RerollCountSpinBox.ValueChanged += OnRerollCountSpinBoxValueChanged;
		QuestionTimeSpinBox.ValueChanged += OnQuestionTimeSpinBoxValueChanged;
		PoolNumberSpinBox.ValueChanged += OnPoolNumberSpinBoxValueChanged;

		ValidateGameModeCards();
	}

	private void ValidateGameModeCards()
	{
		GD.Print($"Registered game modes: {string.Join(", ", GameModeRegistry.All.Select(mode => mode.Id))}.");

		foreach (var node in FindChildren("*", "", true, false))
		{
			if (node is GameModeCard card && !GameModeRegistry.IsRegistered(card.GameModeType))
			{
				GD.PushWarning(
					$"Game mode card '{card.GetPath()}' uses unregistered mode index '{(int)card.GameModeType}'.");
			}
		}
	}

	public override void _ExitTree()
	{
		GameManager.Instance.InitialRerollCount = (int)RerollCountSpinBox.Value;
		GameManager.Instance.RerollCount = (int)RerollCountSpinBox.Value;
		GameManager.Instance.QuestionTime = (int)QuestionTimeSpinBox.Value;
		GameManager.Instance.SelectedPoolNumber = (int)PoolNumberSpinBox.Value;

		RerollCountSpinBox.ValueChanged -= OnRerollCountSpinBoxValueChanged;
		QuestionTimeSpinBox.ValueChanged -= OnQuestionTimeSpinBoxValueChanged;
		PoolNumberSpinBox.ValueChanged -= OnPoolNumberSpinBoxValueChanged;
	}

	private void OnRerollCountSpinBoxValueChanged(double value)
	{
		GameManager.Instance.InitialRerollCount = (int)value;
	}
	private void OnQuestionTimeSpinBoxValueChanged(double value)
	{
		GameManager.Instance.QuestionTime = (int)value;
	}
	private void OnPoolNumberSpinBoxValueChanged(double value)
	{
		var poolNumber = (int)value;
		GameManager.Instance.SelectedPoolNumber = poolNumber;
		QuestionsManager.Instance.LoadQuestionsFromPool(poolNumber);
	}
}
