using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using eyecarebyzewzack.Models;

namespace eyecarebyzewzack.Services;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly EyeCareTimer _timer;
    private readonly Action _onOpenWindow;
    private readonly Action _onOpenSettings;
    private readonly Action _onExit;

    private Icon? _currentIcon;

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

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "EyeCare - Göz ve Duruş Takipçisi"
        };

        BuildContextMenu();
        UpdateIcon(TimerMode.Working, false);

        _notifyIcon.DoubleClick += (s, e) => _onOpenWindow();
    }

    private void BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var itemOpen = new ToolStripMenuItem("👁 Uygulamayı Aç", null, (s, e) => _onOpenWindow());
        itemOpen.Font = new Font(itemOpen.Font, FontStyle.Bold);

        var itemPause = new ToolStripMenuItem("⏸ Duraklat / Devam Et", null, (s, e) => _timer.TogglePause());
        var itemBreak = new ToolStripMenuItem("☕ Mola Ver (5 Dk)", null, (s, e) => _timer.StartBreak(5));
        var itemExt5 = new ToolStripMenuItem("⏳ +5 Dk Uzat", null, (s, e) => _timer.ExtendSession(5));
        var itemExt10 = new ToolStripMenuItem("⏳ +10 Dk Uzat", null, (s, e) => _timer.ExtendSession(10));
        var itemReset = new ToolStripMenuItem("🔄 Oturumu Sıfırla", null, (s, e) => _timer.ResetSession());
        var itemSettings = new ToolStripMenuItem("⚙ Ayarlar", null, (s, e) => _onOpenSettings());
        var itemExit = new ToolStripMenuItem("❌ Çıkış", null, (s, e) => _onExit());

        menu.Items.Add(itemOpen);
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
        _notifyIcon.Text = text;
    }

    public void UpdateIcon(TimerMode mode, bool isIdle)
    {
        System.Drawing.Color dotColor;
        if (isIdle || mode == TimerMode.Paused)
        {
            dotColor = System.Drawing.Color.FromArgb(156, 163, 175); // Gray
        }
        else if (mode == TimerMode.Break)
        {
            dotColor = System.Drawing.Color.FromArgb(99, 102, 241); // Indigo
        }
        else if (mode == TimerMode.Overtime)
        {
            dotColor = System.Drawing.Color.FromArgb(239, 68, 68); // Red
        }
        else
        {
            int rem = _timer.State.RemainingWorkingSeconds;
            if (rem <= 10 * 60)
            {
                dotColor = System.Drawing.Color.FromArgb(245, 158, 11); // Amber
            }
            else
            {
                dotColor = System.Drawing.Color.FromArgb(16, 185, 129); // Emerald Green
            }
        }

        // Generate dynamic crisp 16x16 icon
        using var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            // Draw eye / circular ring
            using var bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(28, 32, 42));
            g.FillEllipse(bgBrush, 0, 0, 15, 15);

            using var ringPen = new Pen(dotColor, 2f);
            g.DrawEllipse(ringPen, 1, 1, 13, 13);

            // Pupil / center dot
            using var dotBrush = new SolidBrush(dotColor);
            g.FillEllipse(dotBrush, 5, 5, 5, 5);
        }

        var oldIcon = _currentIcon;
        IntPtr hIcon = bitmap.GetHicon();
        _currentIcon = Icon.FromHandle(hIcon);
        _notifyIcon.Icon = _currentIcon;

        oldIcon?.Dispose();
    }

    public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(4000, title, message, icon);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _currentIcon?.Dispose();
    }
}
