using Microsoft.Toolkit.Uwp.Notifications;
using System;

namespace Palisades.Helpers
{
    public static class ToastHelper
    {
        public static void ShowMailNotification(string folderName, int newCount)
        {
            if (newCount <= 0) return;
            try
            {
                new ToastContentBuilder()
                    .AddText($"{newCount} new message{(newCount > 1 ? "s" : "")} in {folderName}")
                    .AddAttributionText("Palisades Mail")
                    .Show();
            }
            catch { }
        }

        public static void ShowEventReminder(string summary, DateTime startTime)
        {
            try
            {
                var timeStr = startTime.ToString("HH:mm");
                new ToastContentBuilder()
                    .AddText(summary)
                    .AddText($"Starts at {timeStr}")
                    .AddAttributionText("Palisades Calendar")
                    .Show();
            }
            catch { }
        }

        public static void Cleanup()
        {
            try { ToastNotificationManagerCompat.Uninstall(); } catch { }
        }
    }
}
