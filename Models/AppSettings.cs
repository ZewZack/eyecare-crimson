using System;
using System.IO;
using System.Text.Json;

namespace eyecarebyzewzack.Models;

public enum NotificationStyle
{
    FloatingToast, // Gamer & workflow friendly: Non-stealing corner toast
    FullScreenDim  // Relaxing full screen overlay
}

public class AppSettings
{
    public int WorkDurationMinutes { get; set; } = 60;
    public int BreakDurationMinutes { get; set; } = 5;
    public bool IdleDetectionEnabled { get; set; } = true;
    public int IdleThresholdMinutes { get; set; } = 3;
    public bool SoundEnabled { get; set; } = true;
    public NotificationStyle NotificationStyle { get; set; } = NotificationStyle.FloatingToast;
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTrayOnClose { get; set; } = true;

    // Daily statistics
    public int TodaySittingMinutes { get; set; } = 0;
    public int TodayCompletedBreaks { get; set; } = 0;
    public string LastSavedDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EyeCareZewZack"
    );

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    // Check if date has changed for daily stats
                    if (settings.LastSavedDate != DateTime.Today.ToString("yyyy-MM-dd"))
                    {
                        settings.TodaySittingMinutes = 0;
                        settings.TodayCompletedBreaks = 0;
                        settings.LastSavedDate = DateTime.Today.ToString("yyyy-MM-dd");
                    }
                    return settings;
                }
            }
        }
        catch
        {
            // Ignore read errors, fallback to defaults
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            if (!Directory.Exists(SettingsDir))
            {
                Directory.CreateDirectory(SettingsDir);
            }

            LastSavedDate = DateTime.Today.ToString("yyyy-MM-dd");
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // Best effort write
        }
    }
}
