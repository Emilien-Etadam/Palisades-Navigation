using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Palisades.View;
using Palisades.ViewModel;

namespace Palisades.Helpers
{
    internal sealed class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;

        public TrayIconManager()
        {
            _notifyIcon = new NotifyIcon();

            Icon? icon = null;
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(exePath))
                    icon = Icon.ExtractAssociatedIcon(exePath);
            }
            catch { }

            if (icon == null)
            {
                try
                {
                    var iconPath = Path.Combine(AppContext.BaseDirectory, "Ressources", "icon.ico");
                    if (File.Exists(iconPath))
                        icon = new Icon(iconPath);
                }
                catch { }
            }

            if (icon == null)
                icon = SystemIcons.Application;

            _notifyIcon.Icon = icon;
            _notifyIcon.Text = "Palisades";
            _notifyIcon.Visible = true;

            var menu = new ContextMenuStrip();
            menu.Items.Add(CreateItem("New fence", () => PalisadesManager.CreatePalisade()));
            menu.Items.Add(CreateItem("New Folder Portal", () => PalisadesManager.ShowCreateFolderPortalDialog()));
            menu.Items.Add(CreateItem("New Task Palisade", () => PalisadesManager.ShowCreateTaskPalisadeDialog()));
            menu.Items.Add(CreateItem("New Calendar Palisade", () => PalisadesManager.ShowCreateCalendarPalisadeDialog()));
            menu.Items.Add(CreateItem("New Mail Palisade", () => PalisadesManager.ShowCreateMailPalisadeDialog()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(CreateItem("Manage Zimbra Accounts", () => new ManageAccountsDialog().ShowDialog()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(CreateItem("About", () =>
            {
                var about = new About { DataContext = new AboutViewModel() };
                about.ShowDialog();
            }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(CreateItem("Quit", () =>
            {
                PalisadesManager.CloseAllPalisades();
                System.Windows.Application.Current.Shutdown();
            }));

            _notifyIcon.ContextMenuStrip = menu;

            _notifyIcon.DoubleClick += (_, _) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var w in PalisadesManager.palisades.Values)
                    {
                        try
                        {
                            w.Show();
                            w.Activate();
                        }
                        catch { }
                    }
                });
            };
        }

        private static ToolStripMenuItem CreateItem(string text, Action action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => action());
            };
            return item;
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
