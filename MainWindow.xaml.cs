using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using eyecarebyzewzack.Models;
using eyecarebyzewzack.Services;
using eyecarebyzewzack.Views;

namespace eyecarebyzewzack;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly EyeCareTimer _timer;
    private readonly TrayIconManager _trayManager;

    private bool _isRealExit = false;
    private NotificationWindow? _activeNotification;
    private BreakWindow? _activeBreakWindow;

    private readonly string[] _healthTips = new[]
    {
        "20-20-20 Kuralı: 20 dakikada bir, 20 saniye boyunca 6 metre uzağa bakın.",
        "Göz Kırpma: Ekrana odaklanırken göz kırpma sıklığı %60 azalır, bilinçli kırpın.",
        "Su Tüketimi: Günlük yeterli su içmek göz kuruluğunu ve yorgunluğu önler.",
        "Ekran Mesafesi: Monitörünüz gözünüzden yaklaşık bir kol mesafesinde (50-70 cm) olmalı.",
        "Ekran Yüksekliği: Monitörün üst kenarı göz hizanızda veya biraz altında olmalı.",
        "Omuz ve Boyun: Saat başı omuzlarınızı 5 kez geriye doğru dairesel esnetin."
    };

    private int _tipIndex = 0;
    private int _tipCounterSeconds = 0;

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        _timer = new EyeCareTimer(_settings);

        _trayManager = new TrayIconManager(
            _timer,
            onOpenWindow: ShowAndActivate,
            onOpenSettings: OpenSettingsWindow,
            onExit: ExitApplication
        );

        _timer.Tick += OnTimerTick;
        _timer.TimeLimitReached += OnTimeLimitReached;
        _timer.BreakCompleted += OnBreakCompleted;
        _timer.StateChanged += OnStateChanged;

        MouseDown += MainWindow_MouseDown;
        Closing += MainWindow_Closing;
        Loaded += MainWindow_Loaded;

        ApplySettingsPreferences();
        _timer.Start();
        UpdateUI();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySettingsPreferences();

        // Check command line arguments for --minimized
        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            if (arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
            {
                Hide();
                Win32Helper.TrimWorkingSet();
                break;
            }
        }
    }

    private void ApplySettingsPreferences()
    {
        // Topmost (Pin)
        Topmost = _settings.AlwaysOnTop;
        UpdatePinVisual();

        // AMOLED Mode
        if (_settings.AmoledMode)
        {
            RootBorder.Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            RootBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F1017"));
        }
        else
        {
            RootBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#08090D"));
            RootBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C1217"));
        }

        UpdateModeButtonsVisual();
    }

    private void UpdatePinVisual()
    {
        if (Topmost)
        {
            BtnPin.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            TxtPinIcon.Foreground = Brushes.White;
            BtnPin.ToolTip = "Ekrana Sabitlendi (Kaldırmak için tıkla)";
        }
        else
        {
            BtnPin.Background = Brushes.Transparent;
            TxtPinIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
            BtnPin.ToolTip = "Ekrana Sabitle (Her Zaman En Üstte)";
        }
    }

    private void UpdateModeButtonsVisual()
    {
        if (_timer.State.Profile == TimerProfile.Rule202020)
        {
            BtnMode202020.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            BtnMode202020.Foreground = Brushes.White;
            BtnMode202020.FontWeight = FontWeights.SemiBold;

            BtnModeHourly.Background = Brushes.Transparent;
            BtnModeHourly.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
            BtnModeHourly.FontWeight = FontWeights.Normal;
        }
        else
        {
            BtnModeHourly.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            BtnModeHourly.Foreground = Brushes.White;
            BtnModeHourly.FontWeight = FontWeights.SemiBold;

            BtnMode202020.Background = Brushes.Transparent;
            BtnMode202020.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
            BtnMode202020.FontWeight = FontWeights.Normal;
        }
    }

    private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnTimerTick()
    {
        UpdateUI();

        _tipCounterSeconds++;
        if (_tipCounterSeconds >= 45)
        {
            _tipCounterSeconds = 0;
            _tipIndex = (_tipIndex + 1) % _healthTips.Length;
            TxtTip.Text = _healthTips[_tipIndex];
        }
    }

    private void OnStateChanged()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        var state = _timer.State;

        // Daily stats
        int sittingHours = _settings.TodaySittingMinutes / 60;
        int sittingMins = _settings.TodaySittingMinutes % 60;
        TxtDailySitting.Text = sittingHours > 0 ? $"{sittingHours} sa {sittingMins} dk" : $"{sittingMins} dk";
        TxtDailyBreaks.Text = $"{_settings.TodayCompletedBreaks} Mola";

        // Extension badge
        if (state.ExtensionSeconds > 0 && state.Mode != TimerMode.Break)
        {
            BadgeExtension.Visibility = Visibility.Visible;
            TxtExtensionBadge.Text = $"+{state.ExtensionSeconds / 60} dk uzatıldı";
        }
        else
        {
            BadgeExtension.Visibility = Visibility.Collapsed;
        }

        // Idle Away Display
        if (state.IsIdleAway)
        {
            SetStatusTheme(
                pillBg: "#1F222E",
                pillColor: "#9CA3AF",
                pillText: "💤 PC Başında Değilsiniz (Boşta)",
                timerText: FormatSeconds(state.RemainingWorkingSeconds),
                timerLabel: "Kalan Süre (Durduruldu)",
                arcColor: "#6B7280",
                arcRatio: state.WorkProgressRatio
            );
            _trayManager.UpdateTooltip($"EyeCare - Boşta ({state.IdleSeconds / 60} dk)");
            _trayManager.UpdateIcon(state.Mode, true);
            return;
        }

        // Paused Display
        if (state.Mode == TimerMode.Paused)
        {
            BtnPause.Content = "▶ Devam Et";
            SetStatusTheme(
                pillBg: "#1F222E",
                pillColor: "#9CA3AF",
                pillText: "⏸ Duraklatıldı",
                timerText: FormatSeconds(state.RemainingWorkingSeconds),
                timerLabel: "Kalan Süre (Duraklatıldı)",
                arcColor: "#4B5563",
                arcRatio: state.WorkProgressRatio
            );
            _trayManager.UpdateTooltip("EyeCare - Duraklatıldı");
            _trayManager.UpdateIcon(TimerMode.Paused, false);
            return;
        }

        BtnPause.Content = "⏸ Duraklat";

        // Break Display
        if (state.Mode == TimerMode.Break)
        {
            int rem = state.RemainingBreakSeconds;
            string breakTitle = state.Profile == TimerProfile.Rule202020 ? "👁 20-20-20 Göz Molası" : "☕ Beden & Göz Molası";
            string breakSubtitle = state.Profile == TimerProfile.Rule202020 ? "6 Metre Uzağa Odaklanın" : "Mola Bitimine Kalan";

            SetStatusTheme(
                pillBg: "#3A0E15",
                pillColor: "#F43F5E",
                pillText: breakTitle,
                timerText: FormatSeconds(rem),
                timerLabel: breakSubtitle,
                arcColor: "#E11D48",
                arcRatio: state.BreakProgressRatio
            );
            _trayManager.UpdateTooltip($"EyeCare - Mola ({FormatSeconds(rem)})");
            _trayManager.UpdateIcon(TimerMode.Break, false);
            return;
        }

        // Overtime Display
        if (state.Mode == TimerMode.Overtime)
        {
            int over = state.OvertimeElapsedSeconds;
            bool isHighOvertime = state.ElapsedWorkingSeconds >= 75 * 60;
            string colorHex = isHighOvertime ? "#EF4444" : "#F97316";
            string bgHex = isHighOvertime ? "#450A11" : "#38120B";

            SetStatusTheme(
                pillBg: bgHex,
                pillColor: colorHex,
                pillText: isHighOvertime ? "⚠️ Kritik Süre Aşımı!" : "⏳ Ek Süre / Aşırı Meşguliyet",
                timerText: $"+{FormatSeconds(over)}",
                timerLabel: "Aşan Süre (Mola Verin)",
                arcColor: colorHex,
                arcRatio: 1.0
            );
            _trayManager.UpdateTooltip($"EyeCare - Aşım: +{FormatSeconds(over)}");
            _trayManager.UpdateIcon(TimerMode.Overtime, false);
            return;
        }

        // Normal Working Display (Profile-Aware)
        int remaining = state.RemainingWorkingSeconds;
        int targetMins = (state.TargetWorkingSeconds + state.ExtensionSeconds) / 60;
        int currentMins = state.ElapsedWorkingSeconds / 60;

        string pillBg = "#260C10";
        string pillColor = "#DC2626";
        string arcColor = "#DC2626";

        string modeTitle = (state.Profile == TimerProfile.Rule202020)
            ? $"20-20-20 Modu ({currentMins} / 20 dk)"
            : $"Odaklanma Seansı ({currentMins} / {targetMins} dk)";

        if (remaining <= (state.Profile == TimerProfile.Rule202020 ? 60 : 10 * 60))
        {
            pillBg = "#38150F";
            pillColor = "#F97316";
            arcColor = "#F97316";
        }

        SetStatusTheme(
            pillBg: pillBg,
            pillColor: pillColor,
            pillText: modeTitle,
            timerText: FormatSeconds(remaining),
            timerLabel: "Kalan Çalışma Süresi",
            arcColor: arcColor,
            arcRatio: state.WorkProgressRatio
        );

        _trayManager.UpdateTooltip($"EyeCare - Kalan: {FormatSeconds(remaining)}");
        _trayManager.UpdateIcon(TimerMode.Working, false);
    }

    private void SetStatusTheme(string pillBg, string pillColor, string pillText,
                                string timerText, string timerLabel, string arcColor, double arcRatio)
    {
        PillStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pillBg));
        TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pillColor));
        DotStatus.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pillColor));
        TxtStatus.Text = pillText;

        TxtTimer.Text = timerText;
        TxtTimerLabel.Text = timerLabel;

        SolidColorBrush stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(arcColor));
        GaugeArc.Stroke = stroke;
        GaugeArc.EndAngle = arcRatio * 360.0;
    }

    private static string FormatSeconds(int totalSeconds)
    {
        int min = totalSeconds / 60;
        int sec = totalSeconds % 60;
        return $"{min:00}:{sec:00}";
    }

    private void OnTimeLimitReached(string message, bool isCritical)
    {
        Dispatcher.Invoke(() =>
        {
            if (_settings.SoundEnabled)
            {
                SoundService.PlayAlertNotification();
            }

            if (_activeNotification != null && _activeNotification.IsLoaded)
            {
                _activeNotification.Close();
            }

            _activeNotification = new NotificationWindow(_timer, message, isCritical);
            _activeNotification.Show();

            _trayManager.ShowBalloon("EyeCare - Dinlenme Vakti", message);
        });
    }

    private void OnBreakCompleted()
    {
        Dispatcher.Invoke(() =>
        {
            if (_settings.SoundEnabled)
            {
                SoundService.PlayBreakComplete();
            }

            string msg = _timer.State.Profile == TimerProfile.Rule202020
                ? "20 saniyelik göz molası tamamlandı! Göz merceğiniz rahatladı."
                : "Mola tamamlandı! Gözleriniz ve bedeniniz dinlendi, yeni seans başladı.";

            _trayManager.ShowBalloon("Tebrikler!", msg);
            UpdateUI();
        });
    }

    private void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        _settings.AlwaysOnTop = Topmost;
        _settings.Save();
        UpdatePinVisual();
    }

    private void BtnModeHourly_Click(object sender, RoutedEventArgs e)
    {
        _timer.SwitchProfile(TimerProfile.HourlySitting);
        UpdateModeButtonsVisual();
        UpdateUI();
    }

    private void BtnMode202020_Click(object sender, RoutedEventArgs e)
    {
        _timer.SwitchProfile(TimerProfile.Rule202020);
        UpdateModeButtonsVisual();
        UpdateUI();
    }

    private void BtnExt5_Click(object sender, RoutedEventArgs e)
    {
        _timer.ExtendSession(5);
        UpdateUI();
    }

    private void BtnExt10_Click(object sender, RoutedEventArgs e)
    {
        _timer.ExtendSession(10);
        UpdateUI();
    }

    private void BtnBreak_Click(object sender, RoutedEventArgs e)
    {
        if (_timer.State.Mode != TimerMode.Break)
        {
            _timer.StartBreak();
        }

        if (_activeBreakWindow == null || !_activeBreakWindow.IsLoaded)
        {
            _activeBreakWindow = new BreakWindow(_timer);
            _activeBreakWindow.Show();
        }
        else
        {
            _activeBreakWindow.Activate();
        }

        UpdateUI();
    }

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        _timer.TogglePause();
        UpdateUI();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        _timer.ResetSession();
        UpdateUI();
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }

    private void OpenSettingsWindow()
    {
        var settingsWindow = new SettingsWindow(_settings, onSettingsUpdated: () =>
        {
            ApplySettingsPreferences();
            _timer.ApplyProfileDurations();
            UpdateUI();
        });
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        Win32Helper.TrimWorkingSet();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        if (_settings.MinimizeToTrayOnClose)
        {
            Hide();
            Win32Helper.TrimWorkingSet();
        }
        else
        {
            ExitApplication();
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isRealExit && _settings.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            Hide();
            Win32Helper.TrimWorkingSet();
        }
    }

    private void ExitApplication()
    {
        _isRealExit = true;
        _timer.Stop();
        _trayManager.Dispose();
        _settings.Save();
        System.Windows.Application.Current.Shutdown();
    }
}