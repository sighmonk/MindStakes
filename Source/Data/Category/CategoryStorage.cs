using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileAccess = Godot.FileAccess;

public static class CategoryStorage
{
    public const string ExamplesPath = "res://Resources/Examples/Categories";
    public const string LegacyPath = "res://Resources/Categories";
    public const string UserPath = "user://categories";
    public const string DirectoryArgumentPrefix = "--categories-dir=";
    public const string EnvironmentVariableName = "MINDSTAKES_CATEGORIES_DIR";

    private const string DirectoryProjectSetting = "mindstakes/categories_dir";
    private const string SeedMarkerName = ".examples_seeded";

    private static string _rootPath;

    public static string RootPath => _rootPath ??= ResolveRootPath();

    public static bool IsExternal => !RootPath.StartsWith("res://", StringComparison.Ordinal);

    public static List<Category> LoadCategories()
    {
        var categories = new List<Category>();
        var rootPath = RootPath;
        var dir = DirAccess.Open(rootPath);
        if (dir == null)
        {
            GD.PushWarning($"Cannot open category folder '{rootPath}'.");
            return categories;
        }

        dir.ListDirBegin();
        var fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (!dir.CurrentIsDir()
                && (fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
                    || fileName.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
                && !fileName.EndsWith(".import", StringComparison.OrdinalIgnoreCase))
            {
                var category = GD.Load<Category>($"{rootPath}/{fileName}");
                if (category != null)
                {
                    categories.Add(category);
                }
                else
                {
                    GD.PushWarning($"Failed to load category '{rootPath}/{fileName}'.");
                }
            }

            fileName = dir.GetNext();
        }

        return categories
            .OrderBy(category => category.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string FindCategoryPath(string nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath))
        {
            return null;
        }

        if (nameOrPath.StartsWith("res://", StringComparison.Ordinal)
            || nameOrPath.StartsWith("user://", StringComparison.Ordinal))
        {
            return nameOrPath;
        }

        var category = LoadCategories()
            .FirstOrDefault(candidate => string.Equals(
                candidate.CategoryName,
                nameOrPath,
                StringComparison.OrdinalIgnoreCase));

        return category?.ResourcePath;
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
            var portablePath = Normalize($"{executableDirectory}/categories");
            if (DirAccess.DirExistsAbsolute(portablePath))
            {
                return portablePath;
            }
        }

        EnsureSeeded(UserPath);
        return UserPath;
    }

    private static void EnsureSeeded(string targetPath)
    {
        var markerPath = $"{targetPath}/{SeedMarkerName}";
        if (FileAccess.FileExists(markerPath))
        {
            return;
        }

        if (HasCategoryFiles(targetPath))
        {
            WriteMarker(markerPath);
            return;
        }

        var sourcePath = HasCategoryFiles(LegacyPath) ? LegacyPath : ExamplesPath;
        if (!DirAccess.DirExistsAbsolute(sourcePath))
        {
            GD.PushWarning($"No categories found at '{sourcePath}'.");
            return;
        }

        var makeDirError = DirAccess.MakeDirRecursiveAbsolute(targetPath);
        if (makeDirError != Error.Ok)
        {
            GD.PushWarning($"Failed to create category folder '{targetPath}'. Error: {makeDirError}");
            return;
        }

        var sourceFiles = DirAccess.GetFilesAt(sourcePath) ?? Array.Empty<string>();
        var seededCount = 0;
        foreach (var fileName in sourceFiles)
        {
            if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var category = GD.Load<Category>($"{sourcePath}/{fileName}");
            if (category == null)
            {
                GD.PushWarning($"Failed to load category '{sourcePath}/{fileName}'.");
                continue;
            }

            if (ResourceSaver.Save(category, $"{targetPath}/{fileName}") == Error.Ok)
            {
                seededCount++;
            }
        }

        if (seededCount > 0)
        {
            WriteMarker(markerPath);
        }

        GD.Print(
            $"Seeded {seededCount} categor{(seededCount == 1 ? "y" : "ies")} " +
            $"from '{sourcePath}' into '{targetPath}'.");
    }

    private static bool HasCategoryFiles(string path)
    {
        if (!DirAccess.DirExistsAbsolute(path))
        {
            return false;
        }

        var files = DirAccess.GetFilesAt(path);
        return files != null
            && files.Any(file => file.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".res", StringComparison.OrdinalIgnoreCase));
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
