using Godot;
using System;

/// <summary>
/// Filter bar for the question list: text search plus category, difficulty,
/// question type and game mode selectors. Every selector keeps its value in the
/// item metadata, so the lists can be rebuilt without losing the selection.
/// </summary>
public partial class QuestionFilterPanel : VBoxContainer
{
    [Signal]
    public delegate void FiltersChangedEventHandler();

    [Export] public LineEdit SearchEdit { get; set; }
    [Export] public Button ResetButton { get; set; }
    [Export] public OptionButton CategoryOption { get; set; }
    [Export] public OptionButton DifficultyOption { get; set; }
    [Export] public OptionButton TypeOption { get; set; }
    [Export] public OptionButton ModeOption { get; set; }

    private static readonly string[] TypeLabelKeys =
    {
        "QUESTION_TYPE_MULTIPLE_CHOICE",
        "QUESTION_TYPE_TRUE_FALSE",
        "QUESTION_TYPE_OPEN_ENDED"
    };

    public override void _Ready()
    {
        SearchEdit.PlaceholderText = Tr("FILTER_SEARCH");
        ResetButton.Text = Tr("FILTER_RESET");

        SearchEdit.TextChanged += _ => EmitFiltersChanged();
        ResetButton.Pressed += ResetFilters;
        CategoryOption.ItemSelected += _ => EmitFiltersChanged();
        DifficultyOption.ItemSelected += _ => EmitFiltersChanged();
        TypeOption.ItemSelected += _ => EmitFiltersChanged();
        ModeOption.ItemSelected += _ => EmitFiltersChanged();

        RefreshOptions();
    }

    public bool HasActiveFilter =>
        !string.IsNullOrWhiteSpace(SearchEdit.Text)
        || CategoryOption.Selected > 0
        || DifficultyOption.Selected > 0
        || TypeOption.Selected > 0
        || ModeOption.Selected > 0;

    /// <summary>Rebuilds every selector, keeping the current selection.</summary>
    public void RefreshOptions()
    {
        RebuildCategories();
        RebuildDifficulties();
        RebuildTypes();
        RebuildModes();
    }

    public bool Matches(Question question)
    {
        if (!HasActiveFilter)
        {
            return true;
        }

        if (question == null)
        {
            return false;
        }

        var search = SearchEdit.Text.Trim();
        if (search.Length > 0
            && (question.QuestionText ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        var category = GetSelectedValue(CategoryOption);
        if (category.Length > 0
            && !string.Equals(question.Category?.CategoryName ?? string.Empty, category, StringComparison.Ordinal))
        {
            return false;
        }

        var difficulty = GetSelectedValue(DifficultyOption);
        if (difficulty.Length > 0
            && !string.Equals(question.Difficulty ?? string.Empty, difficulty, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var type = GetSelectedValue(TypeOption);
        if (type.Length > 0 && type != ((int)question.Type).ToString())
        {
            return false;
        }

        var mode = GetSelectedValue(ModeOption);
        return mode.Length == 0 || question.IsTargetedAt(mode);
    }

    private void ResetFilters()
    {
        SearchEdit.Text = string.Empty;
        CategoryOption.Select(0);
        DifficultyOption.Select(0);
        TypeOption.Select(0);
        ModeOption.Select(0);
        EmitFiltersChanged();
    }

    private void EmitFiltersChanged() => EmitSignal(SignalName.FiltersChanged);

    private void RebuildCategories()
    {
        var previous = GetSelectedValue(CategoryOption);
        CategoryOption.Clear();
        AddOption(CategoryOption, Tr("FILTER_ALL_CATEGORIES"), string.Empty);

        foreach (var category in CategoriesManager.Instance.GetAllCategories())
        {
            AddOption(CategoryOption, $"{Tr("FILTER_CATEGORY")}: {category.CategoryName}", category.CategoryName);
        }

        SelectValue(CategoryOption, previous);
    }

    private void RebuildDifficulties()
    {
        var previous = GetSelectedValue(DifficultyOption);
        DifficultyOption.Clear();
        AddOption(DifficultyOption, Tr("FILTER_ALL_DIFFICULTIES"), string.Empty);

        foreach (var level in QuestionSettings.GetDifficultyLevels())
        {
            AddOption(DifficultyOption, $"{Tr("FILTER_DIFFICULTY")}: {level}", level);
        }

        SelectValue(DifficultyOption, previous);
    }

    private void RebuildTypes()
    {
        var previous = GetSelectedValue(TypeOption);
        TypeOption.Clear();
        AddOption(TypeOption, Tr("FILTER_ALL_TYPES"), string.Empty);

        for (var i = 0; i < TypeLabelKeys.Length; i++)
        {
            AddOption(TypeOption, $"{Tr("FILTER_TYPE")}: {Tr(TypeLabelKeys[i])}", i.ToString());
        }

        SelectValue(TypeOption, previous);
    }

    private void RebuildModes()
    {
        var previous = GetSelectedValue(ModeOption);
        ModeOption.Clear();
        AddOption(ModeOption, Tr("FILTER_ALL_MODES"), string.Empty);

        foreach (var mode in GameModeRegistry.All)
        {
            AddOption(ModeOption, $"{Tr("FILTER_MODE")}: {Tr(mode.DisplayNameKey)}", mode.Id);
        }

        SelectValue(ModeOption, previous);
    }

    private static void AddOption(OptionButton option, string label, string value)
    {
        var index = option.ItemCount;
        option.AddItem(label);
        option.SetItemMetadata(index, value);
    }

    private static string GetSelectedValue(OptionButton option)
    {
        if (option.Selected < 0)
        {
            return string.Empty;
        }

        var metadata = option.GetItemMetadata(option.Selected);
        return metadata.VariantType == Variant.Type.Nil ? string.Empty : metadata.AsString();
    }

    private static void SelectValue(OptionButton option, string value)
    {
        for (var i = 0; i < option.ItemCount; i++)
        {
            var metadata = option.GetItemMetadata(i);
            if (metadata.VariantType != Variant.Type.Nil && metadata.AsString() == value)
            {
                option.Select(i);
                return;
            }
        }

        option.Select(0);
    }
}
