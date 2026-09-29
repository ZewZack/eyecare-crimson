using System;
using System.Windows;
using System.Windows.Input;
using eyecarebyzewzack.Models;
using eyecarebyzewzack.Services;

namespace eyecarebyzewzack.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action _onSettingsUpdated;

    public SettingsWindow(AppSettings settings, Action onSettingsUpdated)
    {
        InitializeComponent();
        _settings = settings;
        _onSettingsUpdated = onSettingsUpdated;

        MouseDown += SettingsWindow_MouseDown;
        LoadSettings();
    }

    private void SettingsWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void LoadSettings()
    {
        // Always on top
        ChkAlwaysOnTop.IsChecked = _settings.AlwaysOnTop;

        // AMOLED Mode
        ChkAmoled.IsChecked = _settings.AmoledMode;

        // Default mode
        SelectComboBoxByTag(CmbDefaultMode, _settings.ActiveProfile.ToString());

        // Work duration
        SelectComboBoxByTag(CmbWorkDuration, _settings.WorkDurationMinutes.ToString());

        // Break duration
        SelectComboBoxByTag(CmbBreakDuration, _settings.BreakDurationMinutes.ToString());

        // 20-20-20 rest duration
        SelectComboBoxByTag(CmbEyeRestDuration, _settings.EyeRestDurationSeconds.ToString());

        // Idle
        ChkIdleDetect.IsChecked = _settings.IdleDetectionEnabled;
        SelectComboBoxByTag(CmbIdleThreshold, _settings.IdleThresholdMinutes.ToString());

        // Sound
        ChkSound.IsChecked = _settings.SoundEnabled;

        // Startup
        ChkStartup.IsChecked = Win32Helper.IsStartupWithWindowsEnabled();

        // Min to tray
        ChkMinToTray.IsChecked = _settings.MinimizeToTrayOnClose;
    }

    private void SelectComboBoxByTag(ComboBox cmb, string tagValue)
    {
        foreach (ComboBoxItem item in cmb.Items)
        {
            if (item.Tag?.ToString() == tagValue)
            {
                cmb.SelectedItem = item;
                break;
            }
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _settings.AlwaysOnTop = ChkAlwaysOnTop.IsChecked ?? false;
        _settings.AmoledMode = ChkAmoled.IsChecked ?? true;

        if (CmbDefaultMode.SelectedItem is ComboBoxItem modeItem &&
            Enum.TryParse<TimerProfile>(modeItem.Tag?.ToString(), out var profile))
        {
            _settings.ActiveProfile = profile;
        }

        if (CmbWorkDuration.SelectedItem is ComboBoxItem workItem &&
            int.TryParse(workItem.Tag?.ToString(), out int workMinutes))
        {
            _settings.WorkDurationMinutes = workMinutes;
        }

        if (CmbBreakDuration.SelectedItem is ComboBoxItem breakItem &&
            int.TryParse(breakItem.Tag?.ToString(), out int breakMinutes))
        {
            _settings.BreakDurationMinutes = breakMinutes;
        }

        if (CmbEyeRestDuration.SelectedItem is ComboBoxItem eyeItem &&
            int.TryParse(eyeItem.Tag?.ToString(), out int eyeSeconds))
        {
            _settings.EyeRestDurationSeconds = eyeSeconds;
        }

        _settings.IdleDetectionEnabled = ChkIdleDetect.IsChecked ?? true;

        if (CmbIdleThreshold.SelectedItem is ComboBoxItem idleItem &&
            int.TryParse(idleItem.Tag?.ToString(), out int idleMinutes))
        {
            _settings.IdleThresholdMinutes = idleMinutes;
        }

        _settings.SoundEnabled = ChkSound.IsChecked ?? true;

        bool startup = ChkStartup.IsChecked ?? false;
        _settings.StartWithWindows = startup;
        Win32Helper.SetStartupWithWindows(startup);

        _settings.MinimizeToTrayOnClose = ChkMinToTray.IsChecked ?? true;

        _settings.Save();
        _onSettingsUpdated();

        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
