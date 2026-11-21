using System;

/// <summary>
/// Describes one game mode: its stable id, the localization key of the name shown in the mode pick
/// screen, the scene opened after a category is picked and the filter for the questions it accepts.
/// </summary>
public sealed class GameModeDefinition
{
    public GameModeDefinition(
        int index,
        string id,
        string displayNameKey,
        string scenePath,
        Func<Question, bool> questionFilter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayNameKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);

        Index = index;
        Id = id;
        DisplayNameKey = displayNameKey;
        ScenePath = scenePath;
        QuestionFilter = questionFilter;
    }

    /// <summary>
    /// Value stored in <see cref="GameManager.CurrentGameModeIndex"/> and in the scenes'
    /// <c>GameModeType</c> export.
    /// </summary>
    public int Index { get; }

    public string Id { get; }

    public string DisplayNameKey { get; }

    public string ScenePath { get; }

    /// <summary>Question filter; <c>null</c> means the mode accepts every question.</summary>
    public Func<Question, bool> QuestionFilter { get; }

    public bool Allows(Question question)
    {
        if (question is null)
        {
            return false;
        }

        return QuestionFilter is null || QuestionFilter(question);
    }
}
