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
            Profile = settings.ActiveProfile
        };

        ApplyProfileDurations();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
    }

    public void ApplyProfileDurations()
    {
        if (State.Profile == TimerProfile.Rule202020)
        {
            State.TargetWorkingSeconds = 20 * 60; // 20 minutes
            State.TargetBreakSeconds = Settings.EyeRestDurationSeconds; // 20 seconds
        }
        else
        {
            State.TargetWorkingSeconds = Settings.WorkDurationMinutes * 60;
            State.TargetBreakSeconds = Settings.BreakDurationMinutes * 60;
        }
    }

    public void SwitchProfile(TimerProfile newProfile)
    {
        State.Profile = newProfile;
        Settings.ActiveProfile = newProfile;
        Settings.Save();

        ApplyProfileDurations();
        ResetSession();
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

    public void StartBreak(int seconds = -1)
    {
        if (seconds > 0)
        {
            State.TargetBreakSeconds = seconds;
        }
        else
        {
            if (State.Profile == TimerProfile.Rule202020)
            {
                State.TargetBreakSeconds = Settings.EyeRestDurationSeconds;
            }
            else
            {
                State.TargetBreakSeconds = Settings.BreakDurationMinutes * 60;
            }
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

                // If user has been away for at least 5 minutes, consider it a natural rest!
                if (_idleCounterSeconds >= 5 * 60)
                {
                    ResetSession();
                    Settings.TodayCompletedBreaks++;
                    Settings.Save();
                    _idleCounterSeconds = 0;
                }

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

        // Working or Overtime
        State.ElapsedWorkingSeconds++;

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

                string msg;
                if (State.Profile == TimerProfile.Rule202020)
                {
                    msg = "20-20-20 Vakti! Gözlerinizi ekrandan ayırıp 20 saniye boyunca en az 6 metre uzağa bakın.";
                }
                else
                {
                    msg = State.ExtensionSeconds > 0
                        ? $"Ek süreniz doldu! Toplam {State.ElapsedWorkingSeconds / 60} dakikadır oturuyorsunuz."
                        : $"Maksimum oturma sınırına ulaştınız! Göz ve omurga sağlığınız için lütfen mola verin.";
                }

                TimeLimitReached?.Invoke(msg, false);
            }
            else
            {
                _overtimeAlertIntervalSeconds++;
                // In 20-20-20 mode, remind every 2 minutes; in hourly mode every 5 minutes
                int reminderInterval = (State.Profile == TimerProfile.Rule202020) ? 2 * 60 : 5 * 60;
                if (_overtimeAlertIntervalSeconds >= reminderInterval)
                {
                    _overtimeAlertIntervalSeconds = 0;
                    bool isCritical = State.ElapsedWorkingSeconds >= 75 * 60;
                    string msg = isCritical
                        ? $"⚠️ DİKKAT: {State.ElapsedWorkingSeconds / 60} dakikadır aralıksız oturuyorsunuz! Lütfen kalkıp dinlenin."
                        : (State.Profile == TimerProfile.Rule202020 
                            ? "Göz Dinlendirme Hatırlatması: 20 saniyelik mola vermeyi unutmayın!" 
                            : $"Hatırlatma: {State.ElapsedWorkingSeconds / 60} dakikadır oturuyorsunuz. Lütfen mola verin.");

                    TimeLimitReached?.Invoke(msg, isCritical);
                }
            }
        }

        Tick?.Invoke();
    }
}
