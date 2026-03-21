using Microsoft.Toolkit.Uwp.Notifications;
using Palisades.Properties;
using System;
using System.Globalization;

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
                    .AddText(string.Format(CultureInfo.CurrentCulture, Strings.ToastNewMessagesFormat, newCount, folderName))
                    .AddAttributionText(Strings.ToastMailAttribution)
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
                    .AddText(string.Format(CultureInfo.CurrentCulture, Strings.ToastStartsAtFormat, timeStr))
                    .AddAttributionText(Strings.ToastCalendarAttribution)
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
