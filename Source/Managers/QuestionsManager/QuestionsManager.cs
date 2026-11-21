using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class QuestionsManager : Node
{
    public static QuestionsManager Instance => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<QuestionsManager>("QuestionsManager");

    public List<Question> Questions { get; private set; } = new();
    public Question RandomQuestion { get; private set; }
    public HashSet<string> UsedQuestionIds { get; } = new(StringComparer.Ordinal);

    public override void _Ready()
    {
        if (QuestionImporter.TryRunFromCommandLine())
        {
            return;
        }

        GD.Print($"Question storage root: '{QuestionStorage.RootPath}'.");
        SetRandomQuestion();

        if (!OS.HasFeature("editor") && !QuestionStorage.IsExternal)
        {
            GD.PushWarning(
                "Questions are loaded from res:// and are bundled into the exported build. " +
                $"Set {QuestionStorage.EnvironmentVariableName} or pass --questions-dir=<path> to keep them outside the project.");
        }
    }

    public bool HasQuestionsForCategory(Category category)
    {
        var gameMode = GameManager.Instance.CurrentGameMode;
        return Questions.Any(q =>
            q.Category == category &&
            !IsQuestionUsed(q) &&
            GameModeRegistry.IsQuestionAllowed(q, gameMode));
    }

    public bool IsQuestionUsed(Question question)
    {
        return question != null && UsedQuestionIds.Contains(question.EnsureStableId());
    }

    public int GetAvailableQuestionCount() => Questions.Count;

    public int GetUsedQuestionCount() => UsedQuestionIds.Count;

    public void ResetUsedQuestions()
    {
        UsedQuestionIds.Clear();
    }

    public void RestoreUsedQuestions(IEnumerable<string> questionIds)
    {
        UsedQuestionIds.Clear();
        if (questionIds == null)
        {
            return;
        }

        foreach (var id in questionIds.Where(id => !string.IsNullOrEmpty(id)))
        {
            UsedQuestionIds.Add(id);
        }
    }

    public void SetRandomQuestion()
    {
        var currentCategory = CategoriesManager.Instance.RandomCategory;
        if (currentCategory == null)
        {
            RandomQuestion = null;
            return;
        }

        var gameMode = GameManager.Instance.CurrentGameMode;
        var availableQuestions = Questions
            .Where(q =>
                q.Category == currentCategory &&
                !IsQuestionUsed(q) &&
                GameModeRegistry.IsQuestionAllowed(q, gameMode))
            .ToList();

        if (availableQuestions.Count == 0)
        {
            RandomQuestion = null;
            return;
        }

        var randomIndex = GD.Randi() % availableQuestions.Count;
        RandomQuestion = availableQuestions[(int)randomIndex];
    }

    public void RemoveCurrentQuestion()
    {
        if (RandomQuestion != null)
        {
            UsedQuestionIds.Add(RandomQuestion.EnsureStableId());
            Questions.Remove(RandomQuestion);
            RandomQuestion = null;
        }
    }

    /// <summary>
    /// Replaces the question shown on screen with another unused question from the same category
    /// and marks the discarded one as used. Does not touch the round, bets or scores.
    /// </summary>
    /// <returns><c>false</c> when there is no question on screen or no replacement is left.</returns>
    public bool RerollCurrentQuestion()
    {
        var currentQuestion = RandomQuestion;
        if (currentQuestion == null)
        {
            return false;
        }

        var gameMode = GameManager.Instance.CurrentGameMode;
        var alternatives = Questions
            .Where(q =>
                q.Category == currentQuestion.Category &&
                q != currentQuestion &&
                !IsQuestionUsed(q) &&
                GameModeRegistry.IsQuestionAllowed(q, gameMode))
            .ToList();

        if (alternatives.Count == 0)
        {
            return false;
        }

        UsedQuestionIds.Add(currentQuestion.EnsureStableId());
        Questions.Remove(currentQuestion);
        RandomQuestion = alternatives[(int)(GD.Randi() % alternatives.Count)];
        return true;
    }

    public int GetMaxPoolNumber()
    {
        var dir = DirAccess.Open(QuestionStorage.RootPath);
        if (dir == null)
        {
            return 1;
        }

        var maxPoolNumber = 0;
        dir.ListDirBegin();
        var entry = dir.GetNext();
        while (!string.IsNullOrEmpty(entry))
        {
            if (dir.CurrentIsDir() && entry.StartsWith("Pool", StringComparison.OrdinalIgnoreCase))
            {
                var numberPart = new string(entry.Where(char.IsDigit).ToArray());
                if (int.TryParse(numberPart, out var poolNumber) && poolNumber > maxPoolNumber)
                {
                    maxPoolNumber = poolNumber;
                }
            }
            entry = dir.GetNext();
        }

        return maxPoolNumber > 0 ? maxPoolNumber : 1;
    }

    public void LoadQuestionsFromPool(int poolNumber)
    {
        Questions.Clear();

        var poolPath = QuestionStorage.GetPoolPath(poolNumber);
        GD.Print($"Loading questions from '{poolPath}'.");

        var files = QuestionStorage.LoadPoolFiles(poolPath);
        if (files == null)
        {
            GD.PrintErr($"Failed to open {poolPath} directory.");
            return;
        }

        var skippedUsedCount = 0;
        foreach (var file in files)
        {
            if (IsQuestionUsed(file.Question))
            {
                skippedUsedCount++;
                continue;
            }

            Questions.Add(file.Question);
        }

        GD.Print(
            $"Loaded {Questions.Count} question(s) from '{poolPath}' " +
            $"(skipped {skippedUsedCount} already used).");
        QuestionValidator.ValidateAndReport($"Pool{poolNumber}", files);
    }
}
