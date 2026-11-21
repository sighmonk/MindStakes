using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

/// <summary>
/// Saves and restores a whole game session: teams with their images, scores, bets, the round,
/// the active mode and the consumed question ids. Exports and imports a <c>.mssave</c> ZIP with a
/// JSON manifest and the team pictures, so a save moves between machines intact.
/// </summary>
public partial class SaveManager : Node
{
    public const string SaveExtension = ".mssave";
    public const string SavesFolder = "user://saves";
    public const string AutosavePath = SavesFolder + "/autosave" + SaveExtension;
    public const string TeamImagesFolder = "user://team_images";

    private const int FormatVersion = 1;
    private const string ManifestName = "manifest.json";
    private const string TeamsFolder = "teams";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static SaveManager Instance => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SaveManager>("SaveManager");

    public sealed class Manifest
    {
        public int Version { get; set; } = FormatVersion;
        public string SavedAt { get; set; } = string.Empty;
        public GameState Game { get; set; }
        public List<TeamState> Teams { get; set; } = new();
        public List<string> ConsumedQuestionIds { get; set; } = new();
    }

    public sealed class GameState
    {
        public int RoundCount { get; set; }
        public int MaxRounds { get; set; }
        public int InitialRerollCount { get; set; }
        public int RerollCount { get; set; }
        public int QuestionTime { get; set; }
        public int SelectedPoolNumber { get; set; }
        public int GameModeIndex { get; set; }
        public int CurrentTeamId { get; set; } = -1;
        public int InitialTeamId { get; set; } = -1;
    }

    public sealed class TeamState
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#ffffff";
        public float Score { get; set; }
        public int Bet { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string ImageFile { get; set; } = string.Empty;
    }

    public override void _Ready()
    {
        DirAccess.MakeDirRecursiveAbsolute(SavesFolder);
    }

    public bool SaveCurrentGame(string path = null) =>
        ExportCurrentGame(string.IsNullOrEmpty(path) ? AutosavePath : path);

    public bool ExportCurrentGame(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            return false;
        }

        if (!zipPath.EndsWith(SaveExtension, StringComparison.OrdinalIgnoreCase))
        {
            zipPath += SaveExtension;
        }

        DirAccess.MakeDirRecursiveAbsolute(zipPath.GetBaseDir());

        var manifest = BuildManifest();
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        var images = CollectTeamImages(manifest);

        using var packer = new ZipPacker();
        if (packer.Open(zipPath) != Error.Ok)
        {
            GD.PushWarning($"Cannot create save archive '{zipPath}'.");
            return false;
        }

        packer.StartFile(ManifestName);
        packer.WriteFile(Encoding.UTF8.GetBytes(json));
        packer.CloseFile();

        foreach (var image in images)
        {
            packer.StartFile($"{TeamsFolder}/{image.Key}");
            packer.WriteFile(image.Value);
            packer.CloseFile();
        }

        packer.Close();
        return true;
    }

    public bool LoadGame(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        Dictionary<string, byte[]> images = null;
        var manifest = IsArchive(path) ? ReadArchive(path, out images) : ReadJsonFile(path);
        if (manifest == null)
        {
            return false;
        }

        if (images != null)
        {
            RestoreTeamImages(manifest, images);
        }

        ApplyManifest(manifest);
        return true;
    }

    public bool HasAutosave() => FileAccess.FileExists(AutosavePath);

    private static Manifest BuildManifest()
    {
        return new Manifest
        {
            SavedAt = Time.GetDatetimeStringFromSystem(),
            Game = new GameState
            {
                RoundCount = GameManager.Instance.RoundCount,
                MaxRounds = GameManager.Instance.MaxRounds,
                InitialRerollCount = GameManager.Instance.InitialRerollCount,
                RerollCount = GameManager.Instance.RerollCount,
                QuestionTime = GameManager.Instance.QuestionTime,
                SelectedPoolNumber = GameManager.Instance.SelectedPoolNumber,
                GameModeIndex = GameManager.Instance.CurrentGameModeIndex,
                CurrentTeamId = TeamsManager.Instance.CurrentTeam?.Id ?? -1,
                InitialTeamId = TeamsManager.Instance.InitialTeam?.Id ?? -1
            },
            Teams = TeamsManager.Instance.Teams.Select(team => new TeamState
            {
                Id = team.Id,
                Name = team.TeamName ?? string.Empty,
                Color = team.Color.ToHtml(true),
                Score = team.Score,
                Bet = team.CurrentBet,
                ImagePath = team.ImagePath ?? string.Empty,
                ImageFile = GetTeamImageName(team) ?? string.Empty
            }).ToList(),
            ConsumedQuestionIds = QuestionsManager.Instance.UsedQuestionIds.ToList()
        };
    }

    private static Dictionary<string, byte[]> CollectTeamImages(Manifest manifest)
    {
        var images = new Dictionary<string, byte[]>();
        foreach (var team in manifest.Teams.Where(team => !string.IsNullOrEmpty(team.ImageFile)))
        {
            var bytes = ReadBytes(team.ImagePath);
            if (bytes != null)
            {
                images[team.ImageFile] = bytes;
            }
        }

        return images;
    }

    private static void RestoreTeamImages(Manifest manifest, Dictionary<string, byte[]> images)
    {
        DirAccess.MakeDirRecursiveAbsolute(TeamImagesFolder);

        foreach (var team in manifest.Teams.Where(team => !string.IsNullOrEmpty(team.ImageFile)))
        {
            if (!images.TryGetValue(team.ImageFile, out var bytes))
            {
                continue;
            }

            var targetPath = $"{TeamImagesFolder}/{team.ImageFile}";
            if (WriteBytes(targetPath, bytes))
            {
                team.ImagePath = targetPath;
            }
        }
    }

    private static string GetTeamImageName(Team team)
    {
        if (string.IsNullOrEmpty(team?.ImagePath) || !FileAccess.FileExists(team.ImagePath))
        {
            return null;
        }

        var extension = Path.GetExtension(team.ImagePath).ToLowerInvariant();
        return $"team_{team.Id}{(string.IsNullOrEmpty(extension) ? ".png" : extension)}";
    }

    private static Manifest ReadJsonFile(string path)
    {
        var bytes = ReadBytes(path);
        return bytes == null ? null : ParseManifest(Encoding.UTF8.GetString(bytes));
    }

    private static bool IsArchive(string path) =>
        path.EndsWith(SaveExtension, StringComparison.OrdinalIgnoreCase);

    private static Manifest ReadArchive(string path, out Dictionary<string, byte[]> images)
    {
        images = new Dictionary<string, byte[]>();

        using var reader = new ZipReader();
        if (reader.Open(path) != Error.Ok)
        {
            GD.PushWarning($"Cannot open save archive '{path}'.");
            return null;
        }

        Manifest manifest = null;
        foreach (var entry in reader.GetFiles())
        {
            var bytes = reader.ReadFile(entry);
            if (bytes == null)
            {
                continue;
            }

            if (entry == ManifestName)
            {
                manifest = ParseManifest(Encoding.UTF8.GetString(bytes));
            }
            else if (entry.StartsWith(TeamsFolder + "/", StringComparison.Ordinal))
            {
                images[entry[(TeamsFolder.Length + 1)..]] = bytes;
            }
        }

        reader.Close();
        return manifest;
    }

    private static Manifest ParseManifest(string json)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<Manifest>(json, JsonOptions);
            if (manifest == null || manifest.Version != FormatVersion)
            {
                GD.PushWarning("Unsupported or empty save file.");
                return null;
            }

            return manifest;
        }
        catch (JsonException exception)
        {
            GD.PushWarning($"Save file is not valid JSON: {exception.Message}");
            return null;
        }
    }

    private static void ApplyManifest(Manifest manifest)
    {
        var game = manifest.Game;
        if (game == null || manifest.Teams.Count == 0)
        {
            GD.PushWarning("Save file has no game state or teams.");
            return;
        }

        GameManager.Instance.RoundCount = game.RoundCount;
        GameManager.Instance.MaxRounds = game.MaxRounds;
        GameManager.Instance.InitialRerollCount = game.InitialRerollCount;
        GameManager.Instance.RerollCount = game.RerollCount;
        GameManager.Instance.QuestionTime = game.QuestionTime;
        GameManager.Instance.SelectedPoolNumber = game.SelectedPoolNumber;
        GameManager.Instance.CurrentGameModeIndex = game.GameModeIndex;

        var teams = manifest.Teams.Select(team => new Team
        {
            Id = team.Id,
            TeamName = team.Name,
            Color = Color.FromHtml(team.Color),
            Score = team.Score,
            CurrentBet = team.Bet,
            ImagePath = team.ImagePath
        }).ToList();

        TeamsManager.Instance.Teams = teams;
        TeamsManager.Instance.CurrentTeam = teams.FirstOrDefault(team => team.Id == game.CurrentTeamId);
        TeamsManager.Instance.InitialTeam = teams.FirstOrDefault(team => team.Id == game.InitialTeamId);

        QuestionsManager.Instance.RestoreUsedQuestions(manifest.ConsumedQuestionIds);
        QuestionsManager.Instance.LoadQuestionsFromPool(GameManager.Instance.SelectedPoolNumber);
        CategoriesManager.Instance.SetUpCategories();
        QuestionsManager.Instance.SetRandomQuestion();

        SceneManager.Instance.ChangeScene("res://Source/Screens/GameModePick/GameModePickScreen.tscn");
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