using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Difficulty levels offered by the Question Creator, stored in <c>user://settings.cfg</c>.
/// </summary>
public static class QuestionSettings
{
    private const string SettingsPath = "user://settings.cfg";
    private const string Section = "questions";
    private const string LevelsKey = "difficulty_levels";

    public static readonly string[] DefaultDifficultyLevels = { "Easy", "Medium", "Hard" };

    public static List<string> GetDifficultyLevels()
    {
        using var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok)
        {
            return new List<string>(DefaultDifficultyLevels);
        }

        var stored = config.GetValue(Section, LevelsKey, new string[0]).AsStringArray();
        var levels = new List<string>();
        foreach (var level in stored)
        {
            var trimmed = level?.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !levels.Contains(trimmed))
            {
                levels.Add(trimmed);
            }
        }

        return levels.Count > 0 ? levels : new List<string>(DefaultDifficultyLevels);
    }

    public static void SaveDifficultyLevels(IEnumerable<string> levels)
    {
        using var config = new ConfigFile();
        config.Load(SettingsPath);
        config.SetValue(Section, LevelsKey, levels.ToArray());

        var error = config.Save(SettingsPath);
        if (error != Error.Ok)
        {
            GD.PushWarning($"Failed to save difficulty levels to '{SettingsPath}'. Error: {error}");
        }
    }
}
