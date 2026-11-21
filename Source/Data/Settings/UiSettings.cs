using Godot;

/// <summary>
/// User interface preferences stored in <c>user://settings.cfg</c>.
/// </summary>
public static class UiSettings
{
    private const string SettingsPath = "user://settings.cfg";
    private const string Section = "ui";
    private const string AnimationsKey = "animations_enabled";

    public static bool AnimationsEnabled
    {
        get
        {
            using var config = new ConfigFile();
            if (config.Load(SettingsPath) != Error.Ok)
            {
                return true;
            }

            return config.GetValue(Section, AnimationsKey, true).AsBool();
        }
        set
        {
            using var config = new ConfigFile();
            config.Load(SettingsPath);
            config.SetValue(Section, AnimationsKey, value);

            var error = config.Save(SettingsPath);
            if (error != Error.Ok)
            {
                GD.PushWarning($"Failed to save UI settings to '{SettingsPath}'. Error: {error}");
            }
        }
    }
}
