namespace eyecarebyzewzack.Models;

public enum TimerMode
{
    Working,    // Normal working / sitting session
    Overtime,   // Limit reached or user requested +5/+10m extension
    Break,      // Resting eyes and body
    Paused      // Manually paused
}

public class SessionState
{
    public TimerProfile Profile { get; set; } = TimerProfile.HourlySitting;
    public TimerMode Mode { get; set; } = TimerMode.Working;
    
    // In seconds
    public int ElapsedWorkingSeconds { get; set; } = 0;
    public int TargetWorkingSeconds { get; set; } = 60 * 60; // 60 minutes default
    public int ExtensionSeconds { get; set; } = 0; // Total extensions added this session
    
    // In break
    public int ElapsedBreakSeconds { get; set; } = 0;
    public int TargetBreakSeconds { get; set; } = 5 * 60; // 5 minutes default
    
    public bool IsIdleAway { get; set; } = false;
    public int IdleSeconds { get; set; } = 0;

    public int RemainingWorkingSeconds
    {
        get
        {
            int totalAllowed = TargetWorkingSeconds + ExtensionSeconds;
            int rem = totalAllowed - ElapsedWorkingSeconds;
            return rem > 0 ? rem : 0;
        }
    }

    public int OvertimeElapsedSeconds
    {
        get
        {
            int totalAllowed = TargetWorkingSeconds + ExtensionSeconds;
            return ElapsedWorkingSeconds > totalAllowed ? ElapsedWorkingSeconds - totalAllowed : 0;
        }
    }

    public int RemainingBreakSeconds
    {
        get
        {
            int rem = TargetBreakSeconds - ElapsedBreakSeconds;
            return rem > 0 ? rem : 0;
        }
    }

    public double WorkProgressRatio
    {
        get
        {
            int totalAllowed = TargetWorkingSeconds + ExtensionSeconds;
            if (totalAllowed <= 0) return 1.0;
            double ratio = (double)ElapsedWorkingSeconds / totalAllowed;
            return ratio > 1.0 ? 1.0 : ratio;
        }
    }

    public double BreakProgressRatio
    {
        get
        {
            if (TargetBreakSeconds <= 0) return 1.0;
            double ratio = (double)ElapsedBreakSeconds / TargetBreakSeconds;
            return ratio > 1.0 ? 1.0 : ratio;
        }
    }
}
