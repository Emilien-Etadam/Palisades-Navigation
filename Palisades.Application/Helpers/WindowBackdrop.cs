using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Palisades.Helpers
{
    public static class WindowBackdrop
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(WindowBackdrop),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window && (bool)e.NewValue)
            {
                if (window.IsLoaded)
                    Apply(window);
                else
                    window.Loaded += (_, _) => Apply(window);
            }
        }

        private static void Apply(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int mica = 2; // DWMSBT_MAINWINDOW = Mica
            DwmSetWindowAttribute(hwnd, 38, ref mica, sizeof(int));

            // DWMWCP_DONOTROUND : sans cela, Windows 11 trace ombre + liseré autour des fenêtres sans chrome / transparentes.
            int corner = 1; // DWMWCP_DONOTROUND (voir DWM_WINDOW_CORNER_PREFERENCE)
            DwmSetWindowAttribute(hwnd, 33, ref corner, sizeof(int));

            int useDark = IsSystemDarkMode() ? 1 : 0;
            DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int));

            window.Background = Brushes.Transparent;
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                return val is int i && i == 0;
            }
            catch { return false; }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
