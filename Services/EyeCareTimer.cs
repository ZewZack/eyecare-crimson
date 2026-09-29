using System;
using System.Windows.Threading;
using eyecarebyzewzack.Models;

namespace eyecarebyzewzack.Services;

public class EyeCareTimer
{
    private readonly DispatcherTimer _timer;
    public AppSettings Settings { get; }
    public SessionState State { get; }

    public event Action? Tick;
    public event Action<string, bool>? TimeLimitReached; // message, isCriticalOvertime
    public event Action? BreakCompleted;
    public event Action? StateChanged;

    private int _idleCounterSeconds = 0;
    private int _minuteAccumulatorSeconds = 0;
    private int _overtimeAlertIntervalSeconds = 0;

    public EyeCareTimer(AppSettings settings)
    {
        Settings = settings;
        State = new SessionState
        {
            TargetWorkingSeconds = settings.WorkDurationMinutes * 60,
            TargetBreakSeconds = settings.BreakDurationMinutes * 60
        };

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
    }

    public void Start()
    {
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }
    }

    public void TogglePause()
    {
        if (State.Mode == TimerMode.Paused)
        {
            State.Mode = TimerMode.Working;
        }
        else
        {
            State.Mode = TimerMode.Paused;
        }
        StateChanged?.Invoke();
    }

    public void ResetSession()
    {
        State.ElapsedWorkingSeconds = 0;
        State.ExtensionSeconds = 0;
        State.Mode = TimerMode.Working;
        _overtimeAlertIntervalSeconds = 0;
        StateChanged?.Invoke();
    }

    public void ExtendSession(int minutes)
    {
        State.ExtensionSeconds += minutes * 60;
        
        // If we were already in overtime, adjust mode
        if (State.RemainingWorkingSeconds > 0)
        {
            State.Mode = TimerMode.Working;
        }
        else
        {
            State.Mode = TimerMode.Overtime;
        }
        
        _overtimeAlertIntervalSeconds = 0;
        StateChanged?.Invoke();
    }

    public void StartBreak(int minutes = -1)
    {
        if (minutes > 0)
        {
            State.TargetBreakSeconds = minutes * 60;
        }
        else
        {
            State.TargetBreakSeconds = Settings.BreakDurationMinutes * 60;
        }

        State.ElapsedBreakSeconds = 0;
        State.Mode = TimerMode.Break;
        StateChanged?.Invoke();
    }

    public void SkipBreak()
    {
        ResetSession();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        // 1. Idle Detection Check
        if (Settings.IdleDetectionEnabled)
        {
            int idleSeconds = Win32Helper.GetIdleTimeSeconds();
            State.IdleSeconds = idleSeconds;

            int idleThresholdSeconds = Settings.IdleThresholdMinutes * 60;
            if (idleSeconds >= idleThresholdSeconds)
            {
                if (!State.IsIdleAway)
                {
                    State.IsIdleAway = true;
                    StateChanged?.Invoke();
                }

                _idleCounterSeconds++;

                // If user has been away for at least the break duration (e.g. 5 mins),
                // consider it an automatic natural rest!
                if (_idleCounterSeconds >= Settings.BreakDurationMinutes * 60)
                {
                    // Automatic rest occurred
                    ResetSession();
                    Settings.TodayCompletedBreaks++;
                    Settings.Save();
                    _idleCounterSeconds = 0;
                }

                // While user is away from PC, do not increment sitting time!
                Tick?.Invoke();
                return;
            }
            else
            {
                if (State.IsIdleAway)
                {
                    State.IsIdleAway = false;
                    _idleCounterSeconds = 0;
                    StateChanged?.Invoke();
                }
            }
        }

        // 2. Mode logic
        if (State.Mode == TimerMode.Paused)
        {
            Tick?.Invoke();
            return;
        }

        if (State.Mode == TimerMode.Break)
        {
            State.ElapsedBreakSeconds++;
            if (State.ElapsedBreakSeconds >= State.TargetBreakSeconds)
            {
                // Break done!
                Settings.TodayCompletedBreaks++;
                Settings.Save();
                ResetSession();
                BreakCompleted?.Invoke();
            }
            Tick?.Invoke();
            return;
        }

        // Mode is Working or Overtime
        State.ElapsedWorkingSeconds++;

        // Track daily sitting time
        _minuteAccumulatorSeconds++;
        if (_minuteAccumulatorSeconds >= 60)
        {
            _minuteAccumulatorSeconds = 0;
            Settings.TodaySittingMinutes++;
            Settings.Save();
        }

        int totalAllowedSeconds = State.TargetWorkingSeconds + State.ExtensionSeconds;

        if (State.ElapsedWorkingSeconds >= totalAllowedSeconds)
        {
            if (State.Mode != TimerMode.Overtime)
            {
                State.Mode = TimerMode.Overtime;
                _overtimeAlertIntervalSeconds = 0;
                StateChanged?.Invoke();

                string msg = State.ExtensionSeconds > 0
                    ? $"Ek süreniz doldu! Toplam {State.ElapsedWorkingSeconds / 60} dakikadır bilgisayar başındasınız."
                    : $"1 saatlik çalışma sınırına ulaştınız! Göz sağlığınız için lütfen mola verin.";

                TimeLimitReached?.Invoke(msg, false);
            }
            else
            {
                _overtimeAlertIntervalSeconds++;
                // Alert every 5 minutes if still sitting in overtime
                if (_overtimeAlertIntervalSeconds >= 5 * 60)
                {
                    _overtimeAlertIntervalSeconds = 0;
                    bool isCritical = State.ElapsedWorkingSeconds >= 75 * 60; // 75+ mins
                    string msg = isCritical
                        ? $"⚠️ DİKKAT: {State.ElapsedWorkingSeconds / 60} dakikadır kalkmadınız! Göz kuruluğu ve omurga sağlığı için hemen mola verin."
                        : $"Hatırlatma: {State.ElapsedWorkingSeconds / 60} dakikadır oturuyorsunuz. Lütfen gözlerinizi dinlendirin.";

                    TimeLimitReached?.Invoke(msg, isCritical);
                }
            }
        }

        Tick?.Invoke();
    }
}
