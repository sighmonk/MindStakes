using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

    public static SaveManager Instance => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SaveManager>("SaveManager");

    public override void _Ready()
    {
        DirAccess.MakeDirRecursiveAbsolute(SavesFolder);
    }

    public bool SaveCurrentGame(string path = null)
    {
        var targetPath = string.IsNullOrEmpty(path) ? AutosavePath : path;
        return ExportCurrentGame(targetPath);
    }

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
        var json = Json.Stringify(manifest, "  ");
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
        var manifest = IsArchive(path) ? ReadArchive(path, out images) : ReadSaveFile(path);
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

    private Godot.Collections.Dictionary BuildManifest()
    {
        var game = new Godot.Collections.Dictionary
        {
            ["roundCount"] = GameManager.Instance.RoundCount,
            ["maxRounds"] = GameManager.Instance.MaxRounds,
            ["initialRerollCount"] = GameManager.Instance.InitialRerollCount,
            ["rerollCount"] = GameManager.Instance.RerollCount,
            ["questionTime"] = GameManager.Instance.QuestionTime,
            ["selectedPoolNumber"] = GameManager.Instance.SelectedPoolNumber,
            ["gameModeIndex"] = GameManager.Instance.CurrentGameModeIndex,
            ["currentTeamId"] = TeamsManager.Instance.CurrentTeam?.Id ?? -1,
            ["initialTeamId"] = TeamsManager.Instance.InitialTeam?.Id ?? -1
        };

        var teams = new Godot.Collections.Array();
        foreach (var team in TeamsManager.Instance.Teams)
        {
            teams.Add(new Godot.Collections.Dictionary
            {
                ["id"] = team.Id,
                ["name"] = team.TeamName ?? string.Empty,
                ["color"] = team.Color.ToHtml(true),
                ["score"] = team.Score,
                ["bet"] = team.CurrentBet,
                ["imagePath"] = team.ImagePath ?? string.Empty,
                ["imageFile"] = GetTeamImageName(team) ?? string.Empty
            });
        }

        var consumed = new Godot.Collections.Array();
        foreach (var id in QuestionsManager.Instance.UsedQuestionIds)
        {
            consumed.Add(id);
        }

        return new Godot.Collections.Dictionary
        {
            ["version"] = FormatVersion,
            ["savedAt"] = Time.GetDatetimeStringFromSystem(),
            ["game"] = game,
            ["teams"] = teams,
            ["consumedQuestionIds"] = consumed
        };
    }

    private static Dictionary<string, byte[]> CollectTeamImages(Godot.Collections.Dictionary manifest)
    {
        var images = new Dictionary<string, byte[]>();
        foreach (var entry in manifest["teams"].AsGodotArray())
        {
            var team = entry.AsGodotDictionary();
            var imageFile = team["imageFile"].AsString();
            var imagePath = team["imagePath"].AsString();

            if (string.IsNullOrEmpty(imageFile) || string.IsNullOrEmpty(imagePath))
            {
                continue;
            }

            var bytes = ReadBytes(imagePath);
            if (bytes != null)
            {
                images[imageFile] = bytes;
            }
        }

        return images;
    }

    private static void RestoreTeamImages(Godot.Collections.Dictionary manifest, Dictionary<string, byte[]> images)
    {
        DirAccess.MakeDirRecursiveAbsolute(TeamImagesFolder);

        foreach (var entry in manifest["teams"].AsGodotArray())
        {
            var team = entry.AsGodotDictionary();
            var imageFile = team["imageFile"].AsString();
            if (string.IsNullOrEmpty(imageFile) || !images.TryGetValue(imageFile, out var bytes))
            {
                continue;
            }

            var targetPath = $"{TeamImagesFolder}/{imageFile}";
            if (WriteBytes(targetPath, bytes))
            {
                team["imagePath"] = targetPath;
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
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".png";
        }

        return $"team_{team.Id}{extension}";
    }

    private static Godot.Collections.Dictionary ReadSaveFile(string path)
    {
        var bytes = ReadBytes(path);
        return bytes == null ? null : ParseManifest(Encoding.UTF8.GetString(bytes));
    }

    private static bool IsArchive(string path) =>
        path.EndsWith(SaveExtension, StringComparison.OrdinalIgnoreCase);

    private static Godot.Collections.Dictionary ReadArchive(string path, out Dictionary<string, byte[]> images)
    {
        images = new Dictionary<string, byte[]>();

        using var reader = new ZipReader();
        if (reader.Open(path) != Error.Ok)
        {
            GD.PushWarning($"Cannot open save archive '{path}'.");
            return null;
        }

        Godot.Collections.Dictionary manifest = null;
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

    private static Godot.Collections.Dictionary ParseManifest(string json)
    {
        var parsed = Json.ParseString(json);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            GD.PushWarning("Save file does not contain a JSON object.");
            return null;
        }

        var manifest = parsed.AsGodotDictionary();
        if (!manifest.ContainsKey("version") || manifest["version"].AsInt32() != FormatVersion)
        {
            GD.PushWarning("Unsupported save version.");
            return null;
        }

        return manifest;
    }

    private static void ApplyManifest(Godot.Collections.Dictionary manifest)
    {
        var game = manifest["game"].AsGodotDictionary();
        GameManager.Instance.RoundCount = game["roundCount"].AsInt32();
        GameManager.Instance.MaxRounds = game["maxRounds"].AsInt32();
        GameManager.Instance.InitialRerollCount = game["initialRerollCount"].AsInt32();
        GameManager.Instance.RerollCount = game["rerollCount"].AsInt32();
        GameManager.Instance.QuestionTime = game["questionTime"].AsInt32();
        GameManager.Instance.SelectedPoolNumber = game["selectedPoolNumber"].AsInt32();
        GameManager.Instance.CurrentGameModeIndex = game["gameModeIndex"].AsInt32();

        var teams = new List<Team>();
        foreach (var entry in manifest["teams"].AsGodotArray())
        {
            var data = entry.AsGodotDictionary();
            teams.Add(new Team
            {
                Id = data["id"].AsInt32(),
                TeamName = data["name"].AsString(),
                Color = Color.FromHtml(data["color"].AsString()),
                Score = (float)data["score"].AsDouble(),
                CurrentBet = data["bet"].AsInt32(),
                ImagePath = data["imagePath"].AsString()
            });
        }

        TeamsManager.Instance.Teams = teams;
        TeamsManager.Instance.CurrentTeam = teams.FirstOrDefault(team => team.Id == game["currentTeamId"].AsInt32());
        TeamsManager.Instance.InitialTeam = teams.FirstOrDefault(team => team.Id == game["initialTeamId"].AsInt32());

        var consumed = manifest["consumedQuestionIds"].AsGodotArray().Select(id => id.AsString());
        QuestionsManager.Instance.RestoreUsedQuestions(consumed);

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
