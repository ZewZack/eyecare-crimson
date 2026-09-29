using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using eyecarebyzewzack.Models;

namespace eyecarebyzewzack.Services;

public class TrayIconManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private readonly NotifyIcon _notifyIcon;
    private readonly EyeCareTimer _timer;
    private readonly Action _onOpenWindow;
    private readonly Action _onOpenSettings;
    private readonly Action _onExit;

    // Pre-cached static icons (Zero GDI leaks)
    private readonly Icon _iconNormal;
    private readonly Icon _iconWarning;
    private readonly Icon _iconOvertime;
    private readonly Icon _iconBreak;
    private readonly Icon _iconIdle;

    private string _lastTooltip = string.Empty;

    public TrayIconManager(
        EyeCareTimer timer,
        Action onOpenWindow,
        Action onOpenSettings,
        Action onExit)
    {
        _timer = timer;
        _onOpenWindow = onOpenWindow;
        _onOpenSettings = onOpenSettings;
        _onExit = onExit;

        // Initialize static cached icons once
        _iconNormal = CreateDotIcon(System.Drawing.Color.FromArgb(220, 38, 38));    // Crimson Red
        _iconWarning = CreateDotIcon(System.Drawing.Color.FromArgb(249, 115, 22));  // Orange Flame
        _iconOvertime = CreateDotIcon(System.Drawing.Color.FromArgb(239, 68, 68)); // Vivid Red
        _iconBreak = CreateDotIcon(System.Drawing.Color.FromArgb(225, 29, 72));    // Ruby Rose
        _iconIdle = CreateDotIcon(System.Drawing.Color.FromArgb(107, 114, 128));   // Slate Gray

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "EyeCare Crimson - Göz ve Duruş Takipçisi",
            Icon = _iconNormal
        };

        BuildContextMenu();
        _notifyIcon.DoubleClick += (s, e) => _onOpenWindow();
    }

    private static Icon CreateDotIcon(System.Drawing.Color dotColor)
    {
        using var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            // Background circle
            using var bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(10, 10, 14));
            g.FillEllipse(bgBrush, 0, 0, 15, 15);

            // Crimson Ring
            using var ringPen = new Pen(dotColor, 2f);
            g.DrawEllipse(ringPen, 1, 1, 13, 13);

            // Pupil
            using var dotBrush = new SolidBrush(dotColor);
            g.FillEllipse(dotBrush, 5, 5, 5, 5);
        }

        IntPtr hIcon = bitmap.GetHicon();
        Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    private void BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var itemOpen = new ToolStripMenuItem("👁 Uygulamayı Aç", null, (s, e) => _onOpenWindow());
        itemOpen.Font = new Font(itemOpen.Font, FontStyle.Bold);

        var itemModeHourly = new ToolStripMenuItem("⏱ 1 Saatlik Seans Modu", null, (s, e) => _timer.SwitchProfile(TimerProfile.HourlySitting));
        var itemMode20 = new ToolStripMenuItem("👁 20-20-20 Göz Kuralı Modu", null, (s, e) => _timer.SwitchProfile(TimerProfile.Rule202020));

        var itemPause = new ToolStripMenuItem("⏸ Duraklat / Devam Et", null, (s, e) => _timer.TogglePause());
        var itemBreak = new ToolStripMenuItem("☕ Mola Ver", null, (s, e) => _timer.StartBreak());
        var itemExt5 = new ToolStripMenuItem("⏳ +5 Dk Uzat", null, (s, e) => _timer.ExtendSession(5));
        var itemExt10 = new ToolStripMenuItem("⏳ +10 Dk Uzat", null, (s, e) => _timer.ExtendSession(10));
        var itemReset = new ToolStripMenuItem("🔄 Oturumu Sıfırla", null, (s, e) => _timer.ResetSession());
        var itemSettings = new ToolStripMenuItem("⚙ Ayarlar", null, (s, e) => _onOpenSettings());
        var itemExit = new ToolStripMenuItem("❌ Çıkış", null, (s, e) => _onExit());

        menu.Items.Add(itemOpen);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(itemModeHourly);
        menu.Items.Add(itemMode20);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(itemBreak);
        menu.Items.Add(itemExt5);
        menu.Items.Add(itemExt10);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(itemPause);
        menu.Items.Add(itemReset);
        menu.Items.Add(itemSettings);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(itemExit);

        _notifyIcon.ContextMenuStrip = menu;
    }

    public void UpdateTooltip(string text)
    {
        if (text.Length > 63)
        {
            text = text.Substring(0, 60) + "...";
        }

        if (_lastTooltip != text)
        {
            _lastTooltip = text;
            try
            {
                _notifyIcon.Text = text;
            }
            catch { }
        }
    }

    public void UpdateIcon(TimerMode mode, bool isIdle)
    {
        Icon targetIcon;
        if (isIdle || mode == TimerMode.Paused)
        {
            targetIcon = _iconIdle;
        }
        else if (mode == TimerMode.Break)
        {
            targetIcon = _iconBreak;
        }
        else if (mode == TimerMode.Overtime)
        {
            targetIcon = _iconOvertime;
        }
        else
        {
            int rem = _timer.State.RemainingWorkingSeconds;
            int warningThreshold = (_timer.State.Profile == TimerProfile.Rule202020) ? 60 : 10 * 60;
            targetIcon = (rem <= warningThreshold) ? _iconWarning : _iconNormal;
        }

        if (!ReferenceEquals(_notifyIcon.Icon, targetIcon))
        {
            try
            {
                _notifyIcon.Icon = targetIcon;
            }
            catch { }
        }
    }

    public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        try
        {
            _notifyIcon.ShowBalloonTip(4000, title, message, icon);
        }
        catch { }
    }

    public void Dispose()
    {
        try
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _iconNormal.Dispose();
            _iconWarning.Dispose();
            _iconOvertime.Dispose();
            _iconBreak.Dispose();
            _iconIdle.Dispose();
        }
        catch { }
    }
}
