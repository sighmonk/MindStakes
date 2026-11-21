using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileAccess = Godot.FileAccess;

public static class QuestionStorage
{
    public const string DefaultRootPath = "res://Resources/Examples/Questions";
    public const string ExamplesPath = "res://Resources/Examples/Questions";
    public const string UserPath = "user://questions";
    public const string DirectoryArgumentPrefix = "--questions-dir=";
    public const string EnvironmentVariableName = "MINDSTAKES_QUESTIONS_DIR";

    private const string DirectoryProjectSetting = "mindstakes/questions_dir";
    private const string SeedMarkerName = ".examples_seeded";

    private static string _rootPath;

    public static string RootPath => _rootPath ??= ResolveRootPath();

    public static bool IsExternal => !RootPath.StartsWith("res://", StringComparison.Ordinal);

    public static string GetPoolPath(int poolNumber) => $"{RootPath}/Pool{poolNumber}";

    public static List<(string FilePath, Question Question)> LoadPoolFiles(string poolPath)
    {
        var dir = DirAccess.Open(poolPath);
        if (dir == null)
        {
            return null;
        }

        var files = new List<(string FilePath, Question Question)>();
        dir.ListDirBegin();
        var fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (!dir.CurrentIsDir()
                && (fileName.EndsWith(".tres") || fileName.EndsWith(".res"))
                && !fileName.EndsWith(".import"))
            {
                var filePath = $"{poolPath}/{fileName}";
                var question = GD.Load<Question>(filePath);
                if (question != null)
                {
                    question.EnsureStableId();
                    files.Add((filePath, question));
                }
            }

            fileName = dir.GetNext();
        }

        return files;
    }

    private static string ResolveRootPath()
    {
        var explicitPath = Normalize(GetArgumentValue())
            ?? Normalize(System.Environment.GetEnvironmentVariable(EnvironmentVariableName))
            ?? Normalize(ProjectSettings.GetSetting(DirectoryProjectSetting).AsString());

        if (!string.IsNullOrEmpty(explicitPath))
        {
            return explicitPath;
        }

        if (!OS.HasFeature("editor"))
        {
            var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath());
            var portablePath = Normalize($"{executableDirectory}/questions");
            if (DirAccess.DirExistsAbsolute(portablePath))
            {
                return portablePath;
            }
        }

        EnsureSeeded(UserPath);
        if (DirAccess.DirExistsAbsolute(UserPath))
        {
            return UserPath;
        }

        return DefaultRootPath;
    }

    private static void EnsureSeeded(string targetPath)
    {
        var markerPath = $"{targetPath}/{SeedMarkerName}";
        if (FileAccess.FileExists(markerPath))
        {
            return;
        }

        if (HasQuestionFiles(targetPath))
        {
            WriteMarker(markerPath);
            return;
        }

        if (!DirAccess.DirExistsAbsolute(ExamplesPath))
        {
            GD.PushWarning($"No example questions found at '{ExamplesPath}'.");
            return;
        }

        var categoriesByName = CategoryStorage.LoadCategories()
            .Where(category => !string.IsNullOrWhiteSpace(category.CategoryName))
            .GroupBy(category => category.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        if (categoriesByName.Count == 0)
        {
            GD.PushWarning("Cannot seed example questions because no categories are available.");
            return;
        }

        var makeDirError = DirAccess.MakeDirRecursiveAbsolute(targetPath);
        if (makeDirError != Error.Ok)
        {
            GD.PushWarning($"Failed to create question folder '{targetPath}'. Error: {makeDirError}");
            return;
        }

        var poolDirectories = DirAccess.GetDirectoriesAt(ExamplesPath) ?? Array.Empty<string>();
        var seededCount = 0;
        foreach (var poolDirectory in poolDirectories)
        {
            var examplePoolPath = $"{ExamplesPath}/{poolDirectory}";
            var targetPoolPath = $"{targetPath}/{poolDirectory}";
            DirAccess.MakeDirRecursiveAbsolute(targetPoolPath);

            var questionFiles = DirAccess.GetFilesAt(examplePoolPath) ?? Array.Empty<string>();
            foreach (var fileName in questionFiles)
            {
                if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var question = GD.Load<Question>($"{examplePoolPath}/{fileName}");
                if (question == null)
                {
                    GD.PushWarning($"Failed to load example question '{examplePoolPath}/{fileName}'.");
                    continue;
                }

                var categoryName = question.Category?.CategoryName;
                if (string.IsNullOrWhiteSpace(categoryName)
                    || !categoriesByName.TryGetValue(categoryName, out var category))
                {
                    GD.PushWarning(
                        $"Cannot seed example question '{examplePoolPath}/{fileName}' because its category is missing.");
                    continue;
                }

                question.Category = category;
                question.EnsureStableId();

                if (ResourceSaver.Save(question, $"{targetPoolPath}/{fileName}") == Error.Ok)
                {
                    seededCount++;
                }
            }
        }

        if (seededCount > 0)
        {
            WriteMarker(markerPath);
        }

        GD.Print($"Seeded {seededCount} example question(s) into '{targetPath}'.");
    }

    private static bool HasQuestionFiles(string path)
    {
        if (!DirAccess.DirExistsAbsolute(path))
        {
            return false;
        }

        var poolDirectories = DirAccess.GetDirectoriesAt(path);
        if (poolDirectories == null)
        {
            return false;
        }

        return poolDirectories.Any(poolDirectory =>
        {
            var files = DirAccess.GetFilesAt($"{path}/{poolDirectory}");
            return files != null
                && files.Any(file => file.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".res", StringComparison.OrdinalIgnoreCase));
        });
    }

    private static void WriteMarker(string markerPath)
    {
        using var file = FileAccess.Open(markerPath, FileAccess.ModeFlags.Write);
        file?.StoreString("1");
    }

    private static string GetArgumentValue()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(DirectoryArgumentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return argument[DirectoryArgumentPrefix.Length..];
            }
        }

        return null;
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var trimmed = path.Trim().Trim('"').Replace('\\', '/').TrimEnd('/');
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
