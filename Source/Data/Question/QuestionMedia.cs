using Godot;
using System;
using System.IO;
using FileAccess = Godot.FileAccess;

public enum QuestionMediaKind
{
    Image,
    Audio
}

/// <summary>
/// Copies question media into the project (or <c>user://</c> when the project folder is read only)
/// and loads it back from <c>res://</c>, <c>user://</c> or an absolute path.
/// </summary>
public static class QuestionMedia
{
    public const string ProjectRoot = "res://Assets/Media/Questions";
    public const string UserRoot = "user://question_media";

    private const string WritableProbePath = ProjectRoot + "/.writable_probe";

    private static bool? projectWritable;

    public static string GetFolder(QuestionMediaKind kind) =>
        kind == QuestionMediaKind.Image ? ProjectRoot + "/Images" : ProjectRoot + "/Audio";

    public static string GetUserFolder(QuestionMediaKind kind) =>
        kind == QuestionMediaKind.Image ? UserRoot + "/Images" : UserRoot + "/Audio";

    /// <summary>
    /// Returns the folder new media is copied into: the project folder when it is writable,
    /// otherwise <c>user://question_media</c>.
    /// </summary>
    public static string GetImportFolder(QuestionMediaKind kind) =>
        IsProjectWritable() ? GetFolder(kind) : GetUserFolder(kind);

    public static bool IsProjectWritable()
    {
        if (projectWritable.HasValue)
        {
            return projectWritable.Value;
        }

        DirAccess.MakeDirRecursiveAbsolute(ProjectRoot);
        using var probe = FileAccess.Open(WritableProbePath, FileAccess.ModeFlags.Write);
        projectWritable = probe != null;
        if (probe != null)
        {
            probe.Close();
            DirAccess.RemoveAbsolute(WritableProbePath);
        }

        return projectWritable.Value;
    }

    /// <summary>
    /// Copies <paramref name="sourcePath"/> into the media folder and returns the stored path.
    /// Files already inside the project are returned unchanged.
    /// </summary>
    public static string Import(string sourcePath, QuestionMediaKind kind, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            error = "No file selected.";
            return null;
        }

        var normalizedSource = sourcePath.Replace('\\', '/');
        if (normalizedSource.StartsWith("res://", StringComparison.Ordinal))
        {
            return normalizedSource;
        }

        if (!FileAccess.FileExists(normalizedSource) && !System.IO.File.Exists(normalizedSource))
        {
            error = $"File not found: {normalizedSource}";
            return null;
        }

        var targetFolder = GetImportFolder(kind);
        var makeError = DirAccess.MakeDirRecursiveAbsolute(targetFolder);
        if (makeError != Error.Ok && makeError != Error.AlreadyExists)
        {
            error = $"Cannot create '{targetFolder}'. Error: {makeError}";
            return null;
        }

        var targetPath = BuildTargetPath(targetFolder, normalizedSource);
        var copyError = DirAccess.CopyAbsolute(normalizedSource, targetPath);
        if (copyError != Error.Ok)
        {
            error = $"Cannot copy '{normalizedSource}' to '{targetPath}'. Error: {copyError}";
            GD.PushWarning(error);
            return null;
        }

        return targetPath;
    }

    private static string BuildTargetPath(string targetFolder, string sourcePath)
    {
        var baseName = Path.GetFileNameWithoutExtension(sourcePath).ToLowerInvariant().Replace(' ', '-');
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = "media";
        }

        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var targetPath = $"{targetFolder}/{baseName}{extension}";
        var counter = 2;
        while (FileAccess.FileExists(targetPath))
        {
            targetPath = $"{targetFolder}/{baseName}-{counter}{extension}";
            counter++;
        }

        return targetPath;
    }

    public static Texture2D LoadTexture(string path, Texture2D embeddedFallback = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            using var image = new Image();
            if (image.Load(path) == Error.Ok)
            {
                return ImageTexture.CreateFromImage(image);
            }

            var resource = ResourceLoader.Load<Texture2D>(path);
            if (resource != null)
            {
                return resource;
            }

            GD.PushWarning($"Failed to load question image from '{path}'.");
        }

        return embeddedFallback;
    }

    public static AudioStream LoadAudio(string path, AudioStream embeddedFallback = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            var stream = LoadAudioFromFile(path);
            if (stream != null)
            {
                return stream;
            }

            var resource = ResourceLoader.Load<AudioStream>(path);
            if (resource != null)
            {
                return resource;
            }

            GD.PushWarning($"Failed to load question audio from '{path}'.");
        }

        return embeddedFallback;
    }

    private static AudioStream LoadAudioFromFile(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return null;
        }

        var bytes = file.GetBuffer((long)file.GetLength());
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => new AudioStreamMP3 { Data = bytes },
            ".ogg" => AudioStreamOggVorbis.LoadFromBuffer(bytes),
            ".wav" => LoadWav(bytes),
            _ => null
        };
    }

    private static AudioStreamWav LoadWav(byte[] bytes)
    {
        if (bytes.Length < 44
            || bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F')
        {
            return null;
        }

        var offset = 12;
        int audioFormat = 1;
        int channels = 1;
        int sampleRate = 44100;
        int bitsPerSample = 16;
        byte[] data = null;

        while (offset + 8 <= bytes.Length)
        {
            var chunkId = System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
            var chunkSize = BitConverter.ToInt32(bytes, offset + 4);
            var body = offset + 8;

            if (chunkId == "fmt " && chunkSize >= 16 && body + 16 <= bytes.Length)
            {
                audioFormat = BitConverter.ToInt16(bytes, body);
                channels = BitConverter.ToInt16(bytes, body + 2);
                sampleRate = BitConverter.ToInt32(bytes, body + 4);
                bitsPerSample = BitConverter.ToInt16(bytes, body + 14);
            }
            else if (chunkId == "data" && body <= bytes.Length)
            {
                var length = Math.Min(chunkSize, bytes.Length - body);
                data = new byte[length];
                Array.Copy(bytes, body, data, 0, length);
            }

            offset = body + chunkSize + (chunkSize % 2);
        }

        if (audioFormat != 1 || data == null)
        {
            return null;
        }

        return new AudioStreamWav
        {
            Format = bitsPerSample == 8
                ? AudioStreamWav.FormatEnum.Format8Bits
                : AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = channels == 2,
            Data = data
        };
    }
}
