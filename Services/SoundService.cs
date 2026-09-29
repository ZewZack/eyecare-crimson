using System;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace eyecarebyzewzack.Services;

public static class SoundService
{
    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    /// <summary>
    /// Plays a subtle, non-intrusive chime notifying the user that a break is needed or extension ended.
    /// </summary>
    public static void PlayAlertNotification()
    {
        Task.Run(() =>
        {
            try
            {
                // Play system asterisk or exclamation
                SystemSounds.Asterisk.Play();
            }
            catch
            {
                try
                {
                    MessageBeep(0x00000040); // MB_ICONINFORMATION
                }
                catch { }
            }
        });
    }

    /// <summary>
    /// Plays a pleasant soft sound when a break is successfully completed.
    /// </summary>
    public static void PlayBreakComplete()
    {
        Task.Run(() =>
        {
            try
            {
                SystemSounds.Question.Play();
            }
            catch
            {
                try
                {
                    MessageBeep(0x00000000); // Simple beep
                }
                catch { }
            }
        });
    }
}
