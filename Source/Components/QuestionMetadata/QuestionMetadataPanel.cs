using Godot;
using System.Collections.Generic;

/// <summary>
/// Difficulty and target game modes for one question. The mode checkboxes are built from
/// <see cref="GameModeRegistry"/>, so a new mode appears here without touching this scene.
/// </summary>
public partial class QuestionMetadataPanel : VBoxContainer
{
    [Signal]
    public delegate void MetadataChangedEventHandler();

    [Export] public Label TypeLabel { get; set; }
    [Export] public OptionButton TypeOption { get; set; }
    [Export] public Label DifficultyLabel { get; set; }
    [Export] public OptionButton DifficultyOption { get; set; }
    [Export] public LineEdit NewLevelEdit { get; set; }
    [Export] public Button AddLevelButton { get; set; }
    [Export] public Button RemoveLevelButton { get; set; }
    [Export] public Label ModesLabel { get; set; }
    [Export] public HFlowContainer ModesContainer { get; set; }

    private readonly List<string> levels = new();
    private readonly Dictionary<string, Button> modeToggles = new();

    public override void _Ready()
    {
        TypeLabel.Text = Tr("QUESTION_TYPE");
        DifficultyLabel.Text = Tr("QUESTION_DIFFICULTY");
        ModesLabel.Text = Tr("QUESTION_MODES");
        NewLevelEdit.PlaceholderText = Tr("QUESTION_NEW_LEVEL");
        AddLevelButton.Text = Tr("QUESTION_ADD_LEVEL");
        RemoveLevelButton.Text = Tr("QUESTION_REMOVE_LEVEL");

        BuildTypeOptions();
        ReloadLevels();
        BuildModeChecks();

        TypeOption.ItemSelected += _ => EmitSignal(SignalName.MetadataChanged);
        DifficultyOption.ItemSelected += _ => EmitSignal(SignalName.MetadataChanged);
        AddLevelButton.Pressed += OnAddLevelPressed;
        RemoveLevelButton.Pressed += OnRemoveLevelPressed;
        NewLevelEdit.TextSubmitted += _ => OnAddLevelPressed();
    }

    private void BuildTypeOptions()
    {
        TypeOption.Clear();
        TypeOption.AddItem(Tr("QUESTION_TYPE_MULTIPLE_CHOICE"));
        TypeOption.AddItem(Tr("QUESTION_TYPE_TRUE_FALSE"));
        TypeOption.AddItem(Tr("QUESTION_TYPE_OPEN_ENDED"));
        TypeOption.Select((int)Question.QuestionType.MultipleChoice);
    }

    public Question.QuestionType GetSelectedType() =>
        (Question.QuestionType)TypeOption.Selected;

    private void BuildModeChecks()
    {
        foreach (var mode in GameModeRegistry.All)
        {
            var toggle = new Button
            {
                Text = Tr(mode.DisplayNameKey),
                ToggleMode = true,
                ButtonPressed = true
            };
            toggle.AddThemeFontSizeOverride("font_size", 28);
            toggle.Toggled += _ => EmitSignal(SignalName.MetadataChanged);
            toggle.Toggled += pressed => ApplyModeToggleStyle(toggle, pressed);
            ModesContainer.AddChild(toggle);
            ApplyModeToggleStyle(toggle, true);
            modeToggles[mode.Id] = toggle;
        }
    }

    /// <summary>
    /// Toggle buttons keep the normal style while pressed, so the selected modes are
    /// painted with the theme's pressed stylebox instead of the default one.
    /// </summary>
    private static void ApplyModeToggleStyle(Button toggle, bool pressed)
    {
        if (pressed)
        {
            toggle.AddThemeStyleboxOverride("normal", toggle.GetThemeStylebox("pressed", "Button"));
        }
        else
        {
            toggle.RemoveThemeStyleboxOverride("normal");
        }
    }

    private void ReloadLevels()
    {
        levels.Clear();
        levels.AddRange(QuestionSettings.GetDifficultyLevels());
        RefreshDifficultyOptions(null);
    }

    private void RefreshDifficultyOptions(string selected)
    {
        DifficultyOption.Clear();
        DifficultyOption.AddItem(Tr("QUESTION_DIFFICULTY_NONE"));

        foreach (var level in levels)
        {
            DifficultyOption.AddItem(level);
        }

        var index = 0;
        if (!string.IsNullOrEmpty(selected))
        {
            var levelIndex = levels.IndexOf(selected);
            if (levelIndex >= 0)
            {
                index = levelIndex + 1;
            }
        }

        DifficultyOption.Select(index);
    }

    private void OnAddLevelPressed()
    {
        var level = NewLevelEdit.Text.Trim();
        if (string.IsNullOrEmpty(level))
        {
            return;
        }

        if (!levels.Contains(level))
        {
            levels.Add(level);
        }

        NewLevelEdit.Text = string.Empty;
        QuestionSettings.SaveDifficultyLevels(levels);
        RefreshDifficultyOptions(level);
        EmitSignal(SignalName.MetadataChanged);
    }

    private void OnRemoveLevelPressed()
    {
        var selected = GetSelectedDifficulty();
        if (string.IsNullOrEmpty(selected) || levels.Count <= 1)
        {
            return;
        }

        levels.Remove(selected);
        QuestionSettings.SaveDifficultyLevels(levels);
        RefreshDifficultyOptions(null);
        EmitSignal(SignalName.MetadataChanged);
    }

    public string GetSelectedDifficulty()
    {
        var index = DifficultyOption.Selected;
        return index > 0 && index - 1 < levels.Count ? levels[index - 1] : string.Empty;
    }

    public void LoadQuestion(Question question)
    {
        TypeOption.Select((int)(question?.Type ?? Question.QuestionType.MultipleChoice));

        var difficulty = question?.Difficulty ?? string.Empty;
        if (!string.IsNullOrEmpty(difficulty) && !levels.Contains(difficulty))
        {
            levels.Add(difficulty);
        }

        RefreshDifficultyOptions(difficulty);

        foreach (var pair in modeToggles)
        {
            var enabled = question == null || question.IsTargetedAt(pair.Key);
            pair.Value.SetPressedNoSignal(enabled);
            ApplyModeToggleStyle(pair.Value, enabled);
        }
    }

    public void ApplyTo(Question question)
    {
        question.Type = GetSelectedType();
        question.Difficulty = GetSelectedDifficulty();

        foreach (var pair in modeToggles)
        {
            question.SetTargetMode(pair.Key, pair.Value.ButtonPressed);
        }

        GameModeRegistry.ApplyLegacyFlags(question);
    }
}
