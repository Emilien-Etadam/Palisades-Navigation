using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using Palisades.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Xml.Serialization;

namespace Palisades
{
    internal static class PalisadesManager
    {
        public static readonly Dictionary<string, Window> palisades = new();

        public static void LoadPalisades()
        {
            string saveDirectory = PDirectory.GetPalisadesDirectory();
            PDirectory.EnsureExists(saveDirectory);

            var loadedConcrete = new List<PalisadeModelBase>();

            foreach (string identifierDirname in Directory.GetDirectories(saveDirectory))
            {
                string stateFile = Path.Combine(identifierDirname, "state.xml");
                if (!File.Exists(stateFile))
                    continue;

                try
                {
                    using var reader = new StreamReader(stateFile);
                    var obj = ViewModelBase.SharedSerializer.Deserialize(reader);
                    if (obj is PalisadeModel legacy)
                    {
                        loadedConcrete.Add(PalisadeModelMigration.ToConcreteModel(legacy));
                    }
                    else if (obj is PalisadeModelBase concrete)
                    {
                        loadedConcrete.Add(concrete);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PalisadesManager] Failed to load {stateFile}: {ex.Message}");
                }
            }

            var grouped = loadedConcrete.Where(m => !string.IsNullOrEmpty(m.GroupId)).ToList();
            var standalone = loadedConcrete.Where(m => string.IsNullOrEmpty(m.GroupId)).ToList();

            foreach (var g in grouped.GroupBy(m => m.GroupId!))
            {
                var groupId = g.Key;
                var ordered = g.OrderBy(m => m.TabOrder).ToList();
                var viewModels = new List<IPalisadeViewModel>();
                foreach (var concrete in ordered)
                {
                    var vm = CreateViewModel(concrete);
                    if (vm != null)
                        viewModels.Add(vm);
                }
                if (viewModels.Count == 0) continue;
                var group = new PalisadeGroup(groupId);
                foreach (var vm in viewModels)
                    group.AddMember(vm);
                var tabbed = new TabbedPalisade(group);
                foreach (var vm in viewModels)
                    palisades[vm.Identifier] = tabbed;
            }

            foreach (PalisadeModelBase concrete in standalone)
            {
                var vm = CreateViewModel(concrete);
                if (vm == null) continue;
                var window = CreateWindowFor(vm);
                palisades.Add(concrete.Identifier, window);
            }
        }

        private static IPalisadeViewModel? CreateViewModel(PalisadeModelBase concrete)
        {
            if (concrete is FolderPortalModel folderModel)
                return new FolderPortalViewModel(folderModel);
            if (concrete is TaskPalisadeModel taskModel)
            {
                string caldavUrl;
                string username;
                string password;
                if (taskModel.ZimbraAccountId is Guid accountId && ZimbraAccountStore.GetById(accountId) is ZimbraAccount account)
                {
                    caldavUrl = account.CalDAVBaseUrl ?? string.Empty;
                    username = account.Email ?? string.Empty;
                    password = CredentialEncryptor.Decrypt(account.EncryptedPassword ?? "");
                }
                else
                {
                    caldavUrl = taskModel.CalDAVUrl ?? string.Empty;
                    username = taskModel.CalDAVUsername ?? string.Empty;
                    password = CredentialEncryptor.Decrypt(taskModel.CalDAVPassword ?? "");
                }
                var client = new CalDAVClient(caldavUrl, username, password);
                var caldavService = new Services.CalDAVService(client);
                return new TaskPalisadeViewModel(taskModel, caldavService);
            }
            if (concrete is CalendarPalisadeModel calModel)
            {
                string calUrl = calModel.CalDAVBaseUrl ?? "";
                string calUser = calModel.CalDAVUsername ?? "";
                string calPass = "";
                if (calModel.ZimbraAccountId is Guid zimbraId && ZimbraAccountStore.GetById(zimbraId) is ZimbraAccount zimbraAcc)
                {
                    calUrl = !string.IsNullOrEmpty(zimbraAcc.CalDAVBaseUrl) ? zimbraAcc.CalDAVBaseUrl : calUrl;
                    calUser = !string.IsNullOrEmpty(zimbraAcc.Email) ? zimbraAcc.Email : calUser;
                    calPass = CredentialEncryptor.Decrypt(zimbraAcc.EncryptedPassword ?? "");
                }
                else
                {
                    calPass = CredentialEncryptor.Decrypt(calModel.CalDAVPassword ?? "");
                }
                var calClient = new CalDAVClient(calUrl, calUser, calPass);
                var calService = new CalendarCalDAVService(calClient);
                return new CalendarPalisadeViewModel(calModel, calService);
            }
            if (concrete is MailPalisadeModel mailModel)
                return new MailPalisadeViewModel(mailModel);
            if (concrete is StandardPalisadeModel standardModel)
                return new PalisadeViewModel(standardModel);
            return null;
        }

        public static void CreatePalisade(int? x = null, int? y = null, int? width = null, int? height = null, string? groupId = null, TabbedPalisade? tabbedWindow = null)
        {
            var model = new StandardPalisadeModel();
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            if (width.HasValue) model.Width = width.Value;
            if (height.HasValue) model.Height = height.Value;
            if (!string.IsNullOrEmpty(groupId))
                model.GroupId = groupId;

            var viewModel = new PalisadeViewModel(model);

            if (!string.IsNullOrEmpty(groupId) && tabbedWindow != null && tabbedWindow.DataContext is PalisadeGroup g)
            {
                g.AddMember(viewModel);
                palisades[viewModel.Identifier] = tabbedWindow;
                viewModel.Save();
                tabbedWindow.TabControl.SelectedItem = viewModel;
                return;
            }

            palisades.Add(viewModel.Identifier, new Palisade(viewModel));
            viewModel.Save();
            palisades[viewModel.Identifier].Show();
        }

        public static void CreateFolderPortal(string rootPath, string title, int? x = null, int? y = null, int? width = null, int? height = null, string? groupId = null, TabbedPalisade? tabbedWindow = null)
        {
            var model = new FolderPortalModel
            {
                Name = title,
                RootPath = rootPath,
                CurrentPath = rootPath
            };
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            if (width.HasValue) model.Width = width.Value;
            if (height.HasValue) model.Height = height.Value;
            if (!string.IsNullOrEmpty(groupId))
                model.GroupId = groupId;

            var viewModel = new FolderPortalViewModel(model);

            if (!string.IsNullOrEmpty(groupId) && tabbedWindow != null && tabbedWindow.DataContext is PalisadeGroup g)
            {
                g.AddMember(viewModel);
                palisades[viewModel.Identifier] = tabbedWindow;
                viewModel.Save();
                tabbedWindow.TabControl.SelectedItem = viewModel;
                return;
            }

            palisades.Add(viewModel.Identifier, new FolderPortal(viewModel));
            viewModel.Save();
            palisades[viewModel.Identifier].Show();
        }

        public static void ShowCreateFolderPortalDialogForGroup(PalisadeGroup group, TabbedPalisade tabbedWindow)
        {
            var dialog = new CreateFolderPortalDialog();
            try { dialog.Owner = tabbedWindow; } catch { }
            if (dialog.ShowDialog() == true)
                CreateFolderPortal(dialog.SelectedPath, dialog.PortalTitle, group.X, group.Y, group.Width, group.Height, group.GroupId, tabbedWindow);
        }

        /// <summary>Ouvre le menu « + » : choix du type de palisade à ajouter comme onglet.</summary>
        public static void RequestAddTab(Window hostWindow, FrameworkElement anchor)
        {
            var menu = new ContextMenu();
            void Add(string header, Action action)
            {
                var item = new MenuItem { Header = header };
                item.Click += (_, _) =>
                {
                    menu.IsOpen = false;
                    action();
                };
                menu.Items.Add(item);
            }

            Add("Fence", () => AddTabFence(hostWindow));
            Add("Folder Portal", () => AddTabFolderPortal(hostWindow));
            Add("Task Palisade", () => AddTabTaskPalisade(hostWindow));
            Add("Calendar Palisade", () => AddTabCalendarPalisade(hostWindow));
            Add("Mail Palisade", () => AddTabMailPalisade(hostWindow));

            menu.PlacementTarget = anchor;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private static void AddTabFence(Window hostWindow)
        {
            if (hostWindow is TabbedPalisade tabbed && tabbed.DataContext is PalisadeGroup g)
            {
                CreatePalisade(g.X, g.Y, g.Width, g.Height, g.GroupId, tabbed);
                return;
            }

            if (hostWindow.DataContext is not IPalisadeViewModel existing)
                return;

            var model = new StandardPalisadeModel
            {
                FenceX = existing.FenceX,
                FenceY = existing.FenceY,
                Width = existing.Width,
                Height = existing.Height,
            };
            var vm = new PalisadeViewModel(model);
            MergeStandaloneIntoTabbedWithNewMember(hostWindow, existing, vm);
        }

        private static void AddTabFolderPortal(Window hostWindow)
        {
            if (hostWindow is TabbedPalisade tabbed && tabbed.DataContext is PalisadeGroup g)
            {
                ShowCreateFolderPortalDialogForGroup(g, tabbed);
                return;
            }

            var dialog = new CreateFolderPortalDialog();
            try { dialog.Owner = hostWindow; } catch { }
            if (dialog.ShowDialog() != true) return;
            if (hostWindow.DataContext is not IPalisadeViewModel existing) return;

            var modelNew = new FolderPortalModel
            {
                Name = dialog.PortalTitle,
                RootPath = dialog.SelectedPath,
                CurrentPath = dialog.SelectedPath,
                FenceX = existing.FenceX,
                FenceY = existing.FenceY,
                Width = existing.Width,
                Height = existing.Height,
            };
            var vmNew = new FolderPortalViewModel(modelNew);
            MergeStandaloneIntoTabbedWithNewMember(hostWindow, existing, vmNew);
        }

        private static void AddTabTaskPalisade(Window hostWindow)
        {
            if (hostWindow is TabbedPalisade tabbed && tabbed.DataContext is PalisadeGroup g)
            {
                ShowCreateTaskPalisadeDialog(g, tabbed);
                return;
            }

            var dialog = new CreateTaskPalisadeDialog();
            try { dialog.Owner = hostWindow; } catch { }
            if (dialog.ShowDialog() != true) return;
            if (hostWindow.DataContext is not IPalisadeViewModel existing) return;

            var listIds = dialog.SelectedTaskListIds ?? new List<string>();
            var vm = BuildTaskPalisadeViewModel(dialog.CalDAVUrl, dialog.Username, dialog.Password, listIds, dialog.PalisadeTitle, existing.FenceX, existing.FenceY, existing.Width, existing.Height);
            MergeStandaloneIntoTabbedWithNewMember(hostWindow, existing, vm);
        }

        private static void AddTabCalendarPalisade(Window hostWindow)
        {
            if (hostWindow is TabbedPalisade tabbed && tabbed.DataContext is PalisadeGroup g)
            {
                ShowCreateCalendarPalisadeDialog(g, tabbed);
                return;
            }

            var dialog = new CreateCalendarPalisadeDialog();
            try { dialog.Owner = hostWindow; } catch { }
            if (dialog.ShowDialog() != true) return;
            if (hostWindow.DataContext is not IPalisadeViewModel existing) return;

            var vm = BuildCalendarPalisadeViewModel(dialog.CalDAVUrl, dialog.Username, dialog.Password, dialog.SelectedCalendarIds, dialog.PalisadeTitle, dialog.ViewMode, dialog.DaysToShow, existing.FenceX, existing.FenceY, existing.Width, existing.Height);
            MergeStandaloneIntoTabbedWithNewMember(hostWindow, existing, vm);
        }

        private static void AddTabMailPalisade(Window hostWindow)
        {
            if (hostWindow is TabbedPalisade tabbed && tabbed.DataContext is PalisadeGroup g)
            {
                ShowCreateMailPalisadeDialog(g, tabbed);
                return;
            }

            var dialog = new CreateMailPalisadeDialog();
            try { dialog.Owner = hostWindow; } catch { }
            if (dialog.ShowDialog() != true) return;
            if (hostWindow.DataContext is not IPalisadeViewModel existing) return;

            var vm = BuildMailPalisadeViewModel(dialog.ImapHost, dialog.ImapPort, dialog.Username, dialog.Password, dialog.SelectedFolders, dialog.PalisadeTitle, dialog.DisplayMode, dialog.PollIntervalMinutes, dialog.WebmailUrl, existing.FenceX, existing.FenceY, existing.Width, existing.Height);
            MergeStandaloneIntoTabbedWithNewMember(hostWindow, existing, vm);
        }

        private static void MergeStandaloneIntoTabbedWithNewMember(Window hostWindow, IPalisadeViewModel existing, IPalisadeViewModel vmNew)
        {
            if (!palisades.TryGetValue(existing.Identifier, out var oldWindow) || oldWindow != hostWindow)
                return;

            var gid = Guid.NewGuid().ToString();
            var group = new PalisadeGroup(gid);
            group.AddMember(existing);
            group.AddMember(vmNew);

            palisades.Remove(existing.Identifier);
            oldWindow.Close();

            var tabbed = new TabbedPalisade(group);
            foreach (var m in group.Members)
                palisades[m.Identifier] = tabbed;
            vmNew.Save();
        }

        internal static void OpenEditDialog(IPalisadeViewModel? vm)
        {
            if (vm == null) return;
            Window? owner = null;
            try { owner = GetWindow(vm.Identifier); } catch { }
            switch (vm)
            {
                case PalisadeViewModel p:
                    new EditPalisade { DataContext = p, Owner = owner }.ShowDialog();
                    break;
                case FolderPortalViewModel f:
                    new EditFolderPortal { DataContext = f, Owner = owner }.ShowDialog();
                    break;
                case TaskPalisadeViewModel t:
                    new EditTaskPalisade(t) { Owner = owner }.ShowDialog();
                    break;
                case CalendarPalisadeViewModel c:
                    new EditCalendarPalisade(c) { Owner = owner }.ShowDialog();
                    break;
            }
        }

        private static TaskPalisadeViewModel BuildTaskPalisadeViewModel(string caldavUrl, string username, string password, List<string> taskListIds, string title, int? x, int? y, int? width, int? height)
        {
            taskListIds = taskListIds ?? new List<string>();
            var model = new TaskPalisadeModel
            {
                Name = title,
                CalDAVUrl = caldavUrl,
                CalDAVUsername = username,
                CalDAVPassword = CredentialEncryptor.Encrypt(password),
                TaskListIds = taskListIds,
                TaskListId = taskListIds.Count > 0 ? taskListIds[0] : string.Empty,
                Width = width ?? 600,
                Height = height ?? 400,
            };
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            if (width.HasValue) model.Width = width.Value;
            if (height.HasValue) model.Height = height.Value;
            var client = new CalDAVClient(caldavUrl, username, password);
            var caldavService = new Services.CalDAVService(client);
            return new TaskPalisadeViewModel(model, caldavService);
        }

        public static void CreateTaskPalisade(string caldavUrl, string username, string password, List<string> taskListIds, string title, int? x = null, int? y = null, int? width = null, int? height = null, string? groupId = null, TabbedPalisade? tabbedWindow = null)
        {
            var viewModel = BuildTaskPalisadeViewModel(caldavUrl, username, password, taskListIds, title, x, y, width, height);

            if (!string.IsNullOrEmpty(groupId) && tabbedWindow != null && tabbedWindow.DataContext is PalisadeGroup g)
            {
                g.AddMember(viewModel);
                palisades[viewModel.Identifier] = tabbedWindow;
                viewModel.Save();
                tabbedWindow.TabControl.SelectedItem = viewModel;
                return;
            }

            palisades.Add(viewModel.Identifier, new TaskPalisade(viewModel));
            viewModel.Save();
            palisades[viewModel.Identifier].Show();
        }

        public static void ShowCreateFolderPortalDialog()
        {
            CreateFolderPortalDialog dialog = new();
            if (dialog.ShowDialog() == true)
            {
                CreateFolderPortal(dialog.SelectedPath, dialog.PortalTitle);
            }
        }

        public static void ShowCreateTaskPalisadeDialog(PalisadeGroup? intoGroup = null, TabbedPalisade? tabbedWindow = null)
        {
            var dialog = new CreateTaskPalisadeDialog();
            try { dialog.Owner = tabbedWindow ?? Application.Current.MainWindow; } catch { }
            if (dialog.ShowDialog() != true) return;
            var listIds = dialog.SelectedTaskListIds ?? new List<string>();
            if (intoGroup != null && tabbedWindow != null)
                CreateTaskPalisade(dialog.CalDAVUrl, dialog.Username, dialog.Password, listIds, dialog.PalisadeTitle, intoGroup.X, intoGroup.Y, intoGroup.Width, intoGroup.Height, intoGroup.GroupId, tabbedWindow);
            else
                CreateTaskPalisade(dialog.CalDAVUrl, dialog.Username, dialog.Password, listIds, dialog.PalisadeTitle);
        }

        private static CalendarPalisadeViewModel BuildCalendarPalisadeViewModel(string caldavUrl, string username, string password, List<string> calendarIds, string title, CalendarViewMode viewMode, int daysToShow, int? x, int? y, int? width, int? height)
        {
            var model = new CalendarPalisadeModel
            {
                Name = title,
                CalDAVBaseUrl = caldavUrl,
                CalDAVUsername = username,
                CalDAVPassword = CredentialEncryptor.Encrypt(password),
                CalendarIds = calendarIds ?? new List<string>(),
                ViewMode = viewMode,
                DaysToShow = daysToShow,
                Width = width ?? 500,
                Height = height ?? 400,
            };
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            if (width.HasValue) model.Width = width.Value;
            if (height.HasValue) model.Height = height.Value;
            var client = new CalDAVClient(caldavUrl, username, password);
            var calendarService = new CalendarCalDAVService(client);
            return new CalendarPalisadeViewModel(model, calendarService);
        }

        public static void CreateCalendarPalisade(string caldavUrl, string username, string password, List<string> calendarIds, string title, CalendarViewMode viewMode, int daysToShow, int? x = null, int? y = null, int? width = null, int? height = null, string? groupId = null, TabbedPalisade? tabbedWindow = null)
        {
            var viewModel = BuildCalendarPalisadeViewModel(caldavUrl, username, password, calendarIds, title, viewMode, daysToShow, x, y, width, height);

            if (!string.IsNullOrEmpty(groupId) && tabbedWindow != null && tabbedWindow.DataContext is PalisadeGroup g)
            {
                g.AddMember(viewModel);
                palisades[viewModel.Identifier] = tabbedWindow;
                viewModel.Save();
                tabbedWindow.TabControl.SelectedItem = viewModel;
                return;
            }

            palisades.Add(viewModel.Identifier, new CalendarPalisade(viewModel));
            viewModel.Save();
            palisades[viewModel.Identifier].Show();
        }

        public static void ShowCreateCalendarPalisadeDialog(PalisadeGroup? intoGroup = null, TabbedPalisade? tabbedWindow = null)
        {
            var dialog = new CreateCalendarPalisadeDialog();
            try { dialog.Owner = tabbedWindow ?? Application.Current.MainWindow; } catch { }
            if (dialog.ShowDialog() != true) return;
            if (intoGroup != null && tabbedWindow != null)
                CreateCalendarPalisade(dialog.CalDAVUrl, dialog.Username, dialog.Password, dialog.SelectedCalendarIds, dialog.PalisadeTitle, dialog.ViewMode, dialog.DaysToShow, intoGroup.X, intoGroup.Y, intoGroup.Width, intoGroup.Height, intoGroup.GroupId, tabbedWindow);
            else
                CreateCalendarPalisade(dialog.CalDAVUrl, dialog.Username, dialog.Password, dialog.SelectedCalendarIds, dialog.PalisadeTitle, dialog.ViewMode, dialog.DaysToShow);
        }

        private static MailPalisadeViewModel BuildMailPalisadeViewModel(string imapHost, int imapPort, string username, string password, List<string> monitoredFolders, string title, MailDisplayMode displayMode, int pollIntervalMinutes, string? webmailUrl, int? x, int? y, int? width, int? height)
        {
            var model = new MailPalisadeModel
            {
                Name = title,
                ImapHost = imapHost,
                ImapPort = imapPort,
                ImapUsername = username,
                ImapPassword = CredentialEncryptor.Encrypt(password),
                MonitoredFolders = monitoredFolders ?? new List<string> { "INBOX" },
                DisplayMode = displayMode,
                PollIntervalMinutes = pollIntervalMinutes,
                WebmailUrl = webmailUrl,
                Width = width ?? 320,
                Height = height ?? 240,
            };
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            if (width.HasValue) model.Width = width.Value;
            if (height.HasValue) model.Height = height.Value;
            return new MailPalisadeViewModel(model);
        }

        public static void CreateMailPalisade(string imapHost, int imapPort, string username, string password, List<string> monitoredFolders, string title, MailDisplayMode displayMode, int pollIntervalMinutes, string? webmailUrl = null, int? x = null, int? y = null, int? width = null, int? height = null, string? groupId = null, TabbedPalisade? tabbedWindow = null)
        {
            var viewModel = BuildMailPalisadeViewModel(imapHost, imapPort, username, password, monitoredFolders, title, displayMode, pollIntervalMinutes, webmailUrl, x, y, width, height);

            if (!string.IsNullOrEmpty(groupId) && tabbedWindow != null && tabbedWindow.DataContext is PalisadeGroup g)
            {
                g.AddMember(viewModel);
                palisades[viewModel.Identifier] = tabbedWindow;
                viewModel.Save();
                tabbedWindow.TabControl.SelectedItem = viewModel;
                return;
            }

            palisades.Add(viewModel.Identifier, new MailPalisade(viewModel));
            viewModel.Save();
            palisades[viewModel.Identifier].Show();
        }

        public static void ShowCreateMailPalisadeDialog(PalisadeGroup? intoGroup = null, TabbedPalisade? tabbedWindow = null)
        {
            var dialog = new CreateMailPalisadeDialog();
            try { dialog.Owner = tabbedWindow ?? Application.Current.MainWindow; } catch { }
            if (dialog.ShowDialog() != true) return;
            if (intoGroup != null && tabbedWindow != null)
                CreateMailPalisade(dialog.ImapHost, dialog.ImapPort, dialog.Username, dialog.Password, dialog.SelectedFolders, dialog.PalisadeTitle, dialog.DisplayMode, dialog.PollIntervalMinutes, dialog.WebmailUrl, intoGroup.X, intoGroup.Y, intoGroup.Width, intoGroup.Height, intoGroup.GroupId, tabbedWindow);
            else
                CreateMailPalisade(dialog.ImapHost, dialog.ImapPort, dialog.Username, dialog.Password, dialog.SelectedFolders, dialog.PalisadeTitle, dialog.DisplayMode, dialog.PollIntervalMinutes, dialog.WebmailUrl);
        }

        public static void DeletePalisade(string identifier)
        {
            if (!palisades.TryGetValue(identifier, out Window? window)) return;

            if (window is TabbedPalisade tabbed && tabbed.DataContext is PalisadeGroup group)
            {
                var member = group.Members.FirstOrDefault(m => m.Identifier == identifier);
                if (member == null) return;
                (member as IDisposable)?.Dispose();
                member.Delete();
                group.RemoveMember(member);
                palisades.Remove(identifier);

                if (group.Members.Count == 0)
                {
                    tabbed.Close();
                    return;
                }
                if (group.Members.Count == 1)
                {
                    var single = group.Members[0];
                    tabbed.Close();
                    var standalone = CreateWindowFor(single);
                    palisades[single.Identifier] = standalone;
                    standalone.Show();
                    single.GroupId = null;
                    return;
                }
                foreach (var m in group.Members)
                    palisades[m.Identifier] = tabbed;
                return;
            }

            if (window.DataContext is IPalisadeViewModel vm)
                vm.Delete();
            (window.DataContext as IDisposable)?.Dispose();
            window.Close();
            palisades.Remove(identifier);
        }

        private static Window CreateWindowFor(IPalisadeViewModel vm) => vm switch
        {
            PalisadeViewModel p => new Palisade(p),
            FolderPortalViewModel f => new FolderPortal(f),
            TaskPalisadeViewModel t => new TaskPalisade(t),
            CalendarPalisadeViewModel c => new CalendarPalisade(c),
            MailPalisadeViewModel m => new MailPalisade(m),
            _ => throw new NotSupportedException($"No window for {vm.GetType().Name}")
        };

        public static Window GetWindow(string identifier)
        {
            palisades.TryGetValue(identifier, out Window? window);
            if (window == null)
            {
                throw new KeyNotFoundException(identifier);
            }
            return window;
        }

        public static Palisade GetPalisade(string identifier)
        {
            var window = GetWindow(identifier);
            if (window is Palisade palisade)
            {
                return palisade;
            }
            throw new KeyNotFoundException(identifier);
        }

        /// <summary>Ferme toutes les palisades et vide le dictionnaire (pour RestoreSnapshot).</summary>
        public static void CloseAllPalisades()
        {
            var windows = palisades.Values.ToList();
            palisades.Clear();
            foreach (var w in windows)
            {
                try { w.Close(); } catch { }
            }
        }

        /// <summary>Recalcule position/taille de toutes les palisades (résolution changée).</summary>
        public static void ApplyRescale(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            const int minW = 200, minH = 100;
            var seen = new HashSet<string>();
            foreach (var kv in palisades.ToList())
            {
                var window = kv.Value;
                if (window is View.TabbedPalisade tabbed && tabbed.DataContext is PalisadeGroup group)
                {
                    foreach (var vm in group.Members)
                    {
                        if (seen.Add(vm.Identifier))
                            RescaleVm(vm, oldWidth, oldHeight, newWidth, newHeight, minW, minH);
                    }
                }
                else if (window.DataContext is IPalisadeViewModel vm && seen.Add(vm.Identifier))
                {
                    RescaleVm(vm, oldWidth, oldHeight, newWidth, newHeight, minW, minH);
                }
            }
        }

        private static void RescaleVm(IPalisadeViewModel vm, int oldW, int oldH, int newW, int newH, int minW, int minH)
        {
            int x = (vm.FenceX * newW) / oldW;
            int y = (vm.FenceY * newH) / oldH;
            int w = Math.Max(minW, (vm.Width * newW) / oldW);
            int h = Math.Max(minH, (vm.Height * newH) / oldH);
            if (x + w > newW) x = Math.Max(0, newW - w);
            if (y + h > newH) y = Math.Max(0, newH - h);
            vm.FenceX = x;
            vm.FenceY = y;
            vm.Width = w;
            vm.Height = h;
        }
    }
}
