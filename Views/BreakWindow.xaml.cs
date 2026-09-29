using System;
using System.Windows;
using System.Windows.Input;
using eyecarebyzewzack.Models;
using eyecarebyzewzack.Services;

namespace eyecarebyzewzack.Views;

public partial class BreakWindow : Window
{
    private readonly EyeCareTimer _timer;

    private readonly (string title, string desc)[] _tips = new[]
    {
        ("💡 20-20-20 Kuralı", "Pencereden dışarı veya en az 6 metre uzağa 20 saniye bakın. Göz merceği kaslarınız bu mesafede tamamen gevşer."),
        ("👁 Bilinçli Göz Kırpma", "Ekrana bakarken göz kırpma sıklığımız %60 azalır. Gözlerinizi 10 kez art arda yavaşça kapatıp açarak nemlenmesini sağlayın."),
        ("🤲 Avuç İçi Dinlendirme (Palming)", "Avuçlarınızı birbirine sürterek ısıtın ve gözlerinize bastırmadan hafifçe kapatın. Karanlık ve sıcaklık retinayı rahatlatır."),
        ("🧘 Boyun ve Omuz Esnetme", "Çenenizi yavaşça göğsünüze yaklaştırın, omuzlarınızı geriye doğru dairesel hareketlerle 5 kez çevirin."),
        ("💧 Su Tüketimi", "Bir bardak su için. Vücudun susuz kalması göz kuruluğunun ve baş ağrılarının en yaygın nedenidir.")
    };

    private int _tipIndex = 0;

    public BreakWindow(EyeCareTimer timer)
    {
        InitializeComponent();
        _timer = timer;

        _timer.Tick += Timer_Tick;
        _timer.BreakCompleted += Timer_BreakCompleted;
        Closed += BreakWindow_Closed;
        MouseDown += BreakWindow_MouseDown;

        UpdateDisplay();
    }

    private void BreakWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void Timer_Tick()
    {
        if (_timer.State.Mode != TimerMode.Break)
        {
            Close();
            return;
        }

        UpdateDisplay();

        // Switch tips every 20 seconds
        if (_timer.State.ElapsedBreakSeconds > 0 && _timer.State.ElapsedBreakSeconds % 20 == 0)
        {
            _tipIndex = (_tipIndex + 1) % _tips.Length;
            TxtExerciseTitle.Text = _tips[_tipIndex].title;
            TxtExerciseDesc.Text = _tips[_tipIndex].desc;
        }
    }

    private void UpdateDisplay()
    {
        int rem = _timer.State.RemainingBreakSeconds;
        int min = rem / 60;
        int sec = rem % 60;
        TxtBreakTime.Text = $"{min:00}:{sec:00}";
    }

    private void Timer_BreakCompleted()
    {
        Dispatcher.Invoke(() =>
        {
            if (_timer.Settings.SoundEnabled)
            {
                SoundService.PlayBreakComplete();
            }
            Close();
        });
    }

    private void BtnFinishBreak_Click(object sender, RoutedEventArgs e)
    {
        _timer.ResetSession();
        _timer.Settings.TodayCompletedBreaks++;
        _timer.Settings.Save();
        Close();
    }

    private void BtnAdd2Min_Click(object sender, RoutedEventArgs e)
    {
        _timer.State.TargetBreakSeconds += 2 * 60;
        UpdateDisplay();
    }

    private void BreakWindow_Closed(object? sender, EventArgs e)
    {
        _timer.Tick -= Timer_Tick;
        _timer.BreakCompleted -= Timer_BreakCompleted;
    }
}
