using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

/// <summary>
/// Exports and imports the whole quiz configuration as a <c>.mstakes</c> ZIP: categories,
/// questions with their media, difficulty levels and the teams with their pictures. Import
/// validates the archive first and writes nothing when it is broken.
/// </summary>
public static class ConfigArchive
{
    public const string ArchiveExtension = ".mstakes";
    public const string ConfigsFolder = "user://configs";
    public const string TeamImagesFolder = "user://team_images";

    private const int FormatVersion = 1;
    private const string ManifestName = "manifest.json";
    private const string MediaFolder = "media";
    private const string TeamsFolder = "teams";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public sealed class Manifest
    {
        public int Version { get; set; } = FormatVersion;
        public string SavedAt { get; set; } = string.Empty;
        public List<string> DifficultyLevels { get; set; } = new();
        public List<CategoryEntry> Categories { get; set; } = new();
        public List<QuestionEntry> Questions { get; set; } = new();
        public List<TeamEntry> Teams { get; set; } = new();
    }

    public sealed class CategoryEntry
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    public sealed class QuestionEntry
    {
        public string Id { get; set; } = string.Empty;
        public int Pool { get; set; } = 1;
        public string Category { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> Answers { get; set; } = new();
        public int Type { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public List<string> Modes { get; set; } = new();
        public string FunFact { get; set; } = string.Empty;
        public string ImageFile { get; set; } = string.Empty;
        public string AudioFile { get; set; } = string.Empty;
        public string VideoFile { get; set; } = string.Empty;
    }

    public sealed class TeamEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#ffffff";
        public float Score { get; set; }
        public string ImageFile { get; set; } = string.Empty;
    }

    public static bool Export(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            return false;
        }

        if (!zipPath.EndsWith(ArchiveExtension, StringComparison.OrdinalIgnoreCase))
        {
            zipPath += ArchiveExtension;
        }

        DirAccess.MakeDirRecursiveAbsolute(zipPath.GetBaseDir());

        var manifest = BuildManifest();
        var files = CollectFiles(manifest);

        using var packer = new ZipPacker();
        if (packer.Open(zipPath) != Error.Ok)
        {
            GD.PushWarning($"Cannot create configuration archive '{zipPath}'.");
            return false;
        }

        WriteEntry(packer, ManifestName, JsonSerializer.Serialize(manifest, JsonOptions));
        foreach (var file in files)
        {
            packer.StartFile(file.Key);
            packer.WriteFile(file.Value);
            packer.CloseFile();
        }

        packer.Close();
        return true;
    }

    public static bool Import(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            return false;
        }

        var entries = ReadEntries(zipPath, out var manifest);
        if (entries == null || manifest == null)
        {
            return false;
        }

        if (!Validate(manifest, entries))
        {
            return false;
        }

        var categoryPathById = StoreCategories(manifest);
        StoreQuestions(manifest, entries, categoryPathById);
        StoreTeams(manifest, entries);

        if (manifest.DifficultyLevels.Count > 0)
        {
            QuestionSettings.SaveDifficultyLevels(manifest.DifficultyLevels);
        }

        CategoriesManager.Instance.SetUpCategories();
        QuestionsManager.Instance.LoadQuestionsFromPool(GameManager.Instance.SelectedPoolNumber);
        QuestionsManager.Instance.SetRandomQuestion();
        return true;
    }

    private static Manifest BuildManifest()
    {
        var manifest = new Manifest
        {
            SavedAt = Time.GetDatetimeStringFromSystem(),
            DifficultyLevels = QuestionSettings.GetDifficultyLevels(),
            Categories = CategoriesManager.Instance.Categories
                .Select(category => new CategoryEntry { Name = category.CategoryName, Enabled = category.IsEnabled })
                .ToList(),
            Teams = TeamsManager.Instance.Teams
                .Select(team => new TeamEntry
                {
                    Name = team.TeamName ?? string.Empty,
                    Color = team.Color.ToHtml(true),
                    Score = team.Score,
                    ImageFile = BuildEntryName(TeamsFolder, (ulong)team.Id, team.ImagePath)
                })
                .ToList()
        };

        var maxPool = QuestionsManager.Instance.GetMaxPoolNumber();
        for (var pool = 1; pool <= maxPool; pool++)
        {
            var files = QuestionStorage.LoadPoolFiles(QuestionStorage.GetPoolPath(pool));
            if (files == null)
            {
                continue;
            }

            foreach (var (_, question) in files)
            {
                var answers = question.AnswersText == null
                    ? new List<string>()
                    : question.AnswersText.ToList();

                manifest.Questions.Add(new QuestionEntry
                {
                    Id = question.EnsureStableId(),
                    Pool = pool,
                    Category = question.Category?.CategoryName ?? string.Empty,
                    Text = question.QuestionText ?? string.Empty,
                    Answers = answers,
                    Type = (int)question.Type,
                    Difficulty = question.Difficulty ?? string.Empty,
                    Modes = question.AllowedGameModeIds?.ToList() ?? new List<string>(),
                    FunFact = question.FunFact ?? string.Empty,
                    ImageFile = BuildEntryName(MediaFolder, question.GetInstanceId(), question.PhotoPath, "images"),
                    AudioFile = BuildEntryName(MediaFolder, question.GetInstanceId(), question.AudioPath, "audio"),
                    VideoFile = BuildEntryName(MediaFolder, question.GetInstanceId(), question.VideoPath, "video")
                });
            }
        }

        return manifest;
    }

    private static Dictionary<string, byte[]> CollectFiles(Manifest manifest)
    {
        var files = new Dictionary<string, byte[]>();

        var maxPool = manifest.Questions.Select(question => question.Pool).DefaultIfEmpty(0).Max();
        for (var pool = 1; pool <= maxPool; pool++)
        {
            var poolFiles = QuestionStorage.LoadPoolFiles(QuestionStorage.GetPoolPath(pool));
            if (poolFiles == null)
            {
                continue;
            }

            foreach (var (_, question) in poolFiles)
            {
                var entry = manifest.Questions.FirstOrDefault(item =>
                    item.Pool == pool && item.Id == question.EnsureStableId());
                if (entry == null)
                {
                    continue;
                }

                AddFile(files, entry.ImageFile, question.PhotoPath);
                AddFile(files, entry.AudioFile, question.AudioPath);
                AddFile(files, entry.VideoFile, question.VideoPath);
            }
        }

        foreach (var team in TeamsManager.Instance.Teams)
        {
            var entry = manifest.Teams.FirstOrDefault(item => item.Name == team.TeamName && item.ImageFile != string.Empty);
            if (entry != null)
            {
                AddFile(files, entry.ImageFile, team.ImagePath);
            }
        }

        return files;
    }

    private static void AddFile(Dictionary<string, byte[]> files, string entryName, string sourcePath)
    {
        if (string.IsNullOrEmpty(entryName) || string.IsNullOrEmpty(sourcePath) || files.ContainsKey(entryName))
        {
            return;
        }

        var bytes = ReadBytes(sourcePath);
        if (bytes != null)
        {
            files[entryName] = bytes;
        }
    }

    private static string BuildEntryName(string folder, ulong id, string sourcePath, string kind = "")
    {
        if (string.IsNullOrEmpty(sourcePath) || !FileAccess.FileExists(sourcePath))
        {
            return string.Empty;
        }

        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".bin";
        }

        var prefix = string.IsNullOrEmpty(kind) ? string.Empty : kind + "-";
        return $"{folder}/{prefix}{id}{extension}";
    }

    private static Dictionary<string, byte[]> ReadEntries(string zipPath, out Manifest manifest)
    {
        manifest = null;
        var entries = new Dictionary<string, byte[]>();

        using var reader = new ZipReader();
        if (reader.Open(zipPath) != Error.Ok)
        {
            GD.PushWarning($"Cannot open configuration archive '{zipPath}'.");
            return null;
        }

        foreach (var name in reader.GetFiles())
        {
            var bytes = reader.ReadFile(name);
            if (bytes != null)
            {
                entries[name] = bytes;
            }
        }

        reader.Close();

        if (!entries.TryGetValue(ManifestName, out var manifestBytes))
        {
            GD.PushWarning("Configuration archive has no manifest.");
            return null;
        }

        try
        {
            manifest = JsonSerializer.Deserialize<Manifest>(Encoding.UTF8.GetString(manifestBytes), JsonOptions);
        }
        catch (JsonException exception)
        {
            GD.PushWarning($"Configuration manifest is not valid JSON: {exception.Message}");
            return null;
        }

        if (manifest == null || manifest.Version != FormatVersion)
        {
            GD.PushWarning("Unsupported configuration archive version.");
            return null;
        }

        return entries;
    }

    private static bool Validate(Manifest manifest, Dictionary<string, byte[]> entries)
    {
        if (manifest.Categories.Count == 0 || manifest.Questions.Count == 0)
        {
            GD.PushWarning("Configuration archive has no categories or questions.");
            return false;
        }

        var missing = manifest.Questions
            .SelectMany(question => new[] { question.ImageFile, question.AudioFile, question.VideoFile })
            .Concat(manifest.Teams.Select(team => team.ImageFile))
            .Where(name => !string.IsNullOrEmpty(name) && !entries.ContainsKey(name))
            .Distinct()
            .ToList();

        if (missing.Count > 0)
        {
            GD.PushWarning($"Configuration archive is missing files: {string.Join(", ", missing)}");
            return false;
        }

        return true;
    }

    private static Dictionary<string, string> StoreCategories(Manifest manifest)
    {
        var folder = CategoryStorage.RootPath;
        DirAccess.MakeDirRecursiveAbsolute(folder);
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in manifest.Categories)
        {
            var category = new Category
            {
                CategoryName = entry.Name,
                IsEnabled = entry.Enabled
            };

            var path = $"{folder}/{SanitizeFileName(entry.Name)}.tres";
            ResourceSaver.Save(category, path);
            paths[entry.Name] = path;
        }

        return paths;
    }

    private static void StoreQuestions(
        Manifest manifest,
        Dictionary<string, byte[]> entries,
        Dictionary<string, string> categoryPaths)
    {
        foreach (var question in manifest.Questions)
        {
            var poolPath = QuestionStorage.GetPoolPath(question.Pool);
            DirAccess.MakeDirRecursiveAbsolute(poolPath);

            RemoveExistingQuestion(poolPath, question.Id);

            var resource = new Question
            {
                Id = question.Id,
                QuestionText = question.Text,
                AnswersText = new Godot.Collections.Array<string>(question.Answers),
                Type = (Question.QuestionType)question.Type,
                Difficulty = string.IsNullOrEmpty(question.Difficulty) ? null : question.Difficulty,
                AllowedGameModeIds = new Godot.Collections.Array<string>(question.Modes),
                FunFact = string.IsNullOrEmpty(question.FunFact) ? null : question.FunFact,
                Category = LoadCategory(question.Category, categoryPaths),
                PhotoPath = StoreMedia(entries, question.ImageFile, QuestionMediaKind.Image),
                AudioPath = StoreMedia(entries, question.AudioFile, QuestionMediaKind.Audio),
                VideoPath = StoreMedia(entries, question.VideoFile, QuestionMediaKind.Video)
            };

            GameModeRegistry.ApplyLegacyFlags(resource);
            ResourceSaver.Save(resource, $"{poolPath}/{SanitizeFileName(question.Id)}.tres");
        }
    }

    private static void RemoveExistingQuestion(string poolPath, string questionId)
    {
        var existing = QuestionStorage.LoadPoolFiles(poolPath);
        if (existing == null)
        {
            return;
        }

        foreach (var (filePath, question) in existing)
        {
            if (question.EnsureStableId() == questionId)
            {
                DirAccess.RemoveAbsolute(filePath);
            }
        }
    }

    private static Category LoadCategory(string name, Dictionary<string, string> categoryPaths)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (categoryPaths.TryGetValue(name, out var path))
        {
            return ResourceLoader.Load<Category>(path);
        }

        return CategoriesManager.Instance.Categories.FirstOrDefault(category => category.CategoryName == name);
    }

    private static string StoreMedia(Dictionary<string, byte[]> entries, string entryName, QuestionMediaKind kind)
    {
        if (string.IsNullOrEmpty(entryName) || !entries.TryGetValue(entryName, out var bytes))
        {
            return null;
        }

        var folder = QuestionMedia.GetImportFolder(kind);
        DirAccess.MakeDirRecursiveAbsolute(folder);
        var path = $"{folder}/{Path.GetFileName(entryName)}";
        return WriteBytes(path, bytes) ? path : null;
    }

    private static void StoreTeams(Manifest manifest, Dictionary<string, byte[]> entries)
    {
        DirAccess.MakeDirRecursiveAbsolute(TeamImagesFolder);

        var teams = new List<Team>();
        for (var i = 0; i < manifest.Teams.Count; i++)
        {
            var entry = manifest.Teams[i];
            var imagePath = string.Empty;

            if (!string.IsNullOrEmpty(entry.ImageFile) && entries.TryGetValue(entry.ImageFile, out var bytes))
            {
                var path = $"{TeamImagesFolder}/{Path.GetFileName(entry.ImageFile)}";
                if (WriteBytes(path, bytes))
                {
                    imagePath = path;
                }
            }

            teams.Add(new Team
            {
                Id = i,
                TeamName = entry.Name,
                Color = Color.FromHtml(entry.Color),
                Score = entry.Score,
                ImagePath = imagePath
            });
        }

        TeamsManager.Instance.Teams = teams;
        TeamsManager.Instance.CurrentTeam = teams.FirstOrDefault();
        TeamsManager.Instance.InitialTeam = teams.FirstOrDefault();
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        return builder.ToString().Trim();
    }

    private static void WriteEntry(ZipPacker packer, string name, string text)
    {
        packer.StartFile(name);
        packer.WriteFile(Encoding.UTF8.GetBytes(text));
        packer.CloseFile();
    }

    private static byte[] ReadBytes(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        return file == null ? null : file.GetBuffer((long)file.GetLength());
    }

    private static bool WriteBytes(string path, byte[] bytes)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushWarning($"Cannot write '{path}'.");
            return false;
        }

        file.StoreBuffer(bytes);
        return true;
    }
}