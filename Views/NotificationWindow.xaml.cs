using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using eyecarebyzewzack.Services;

namespace eyecarebyzewzack.Views;

public partial class NotificationWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOPMOST = 0x00000008;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private readonly EyeCareTimer _timer;
    private readonly DispatcherTimer _autoCloseTimer;

    public NotificationWindow(EyeCareTimer timer, string message, bool isCritical)
    {
        InitializeComponent();
        _timer = timer;

        TxtMessage.Text = message;
        if (isCritical)
        {
            TxtTitle.Text = "⚠️ KRİTİK GÖZ & DURUŞ UYARISI";
            TxtSubtitle.Text = "Çok uzun süredir kalkmadınız!";
        }

        // Auto close after 25 seconds if not interacted
        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(25)
        };
        _autoCloseTimer.Tick += (s, e) =>
        {
            _autoCloseTimer.Stop();
            Close();
        };
        _autoCloseTimer.Start();

        Loaded += NotificationWindow_Loaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Make window non-focusable so games / active windows won't lose focus
        var helper = new WindowInteropHelper(this);
        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
        SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
    }

    private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Position at bottom-right of primary screen
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 10;
        Top = workArea.Bottom - Height - 10;
    }

    private void BtnBreak_Click(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer.Stop();
        _timer.StartBreak();
        Close();
    }

    private void BtnExt5_Click(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer.Stop();
        _timer.ExtendSession(5);
        Close();
    }

    private void BtnExt10_Click(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer.Stop();
        _timer.ExtendSession(10);
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer.Stop();
        Close();
    }
}
