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
            var baseSerializer = new XmlSerializer(typeof(PalisadeModelBase), new[]
            {
                typeof(PalisadeModel),
                typeof(StandardPalisadeModel),
                typeof(FolderPortalModel),
                typeof(TaskPalisadeModel),
                typeof(CalendarPalisadeModel),
                typeof(MailPalisadeModel)
            });

            foreach (string identifierDirname in Directory.GetDirectories(saveDirectory))
            {
                string stateFile = Path.Combine(identifierDirname, "state.xml");
                if (!File.Exists(stateFile))
                    continue;

                try
                {
                    using var reader = new StreamReader(stateFile);
                    var obj = baseSerializer.Deserialize(reader);
                    if (obj is PalisadeModel legacy)
                    {
                        loadedConcrete.Add(PalisadeModelMigration.ToConcreteModel(legacy));
                    }
                    else if (obj is PalisadeModelBase concrete)
                    {
                        loadedConcrete.Add(concrete);
                    }
                }
                catch
                {
                    // Skip corrupted state files
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
                Window window = concrete switch
                {
                    FolderPortalModel _ => new FolderPortal((FolderPortalViewModel)vm),
                    TaskPalisadeModel _ => new TaskPalisade((TaskPalisadeViewModel)vm),
                    CalendarPalisadeModel _ => new CalendarPalisade((CalendarPalisadeViewModel)vm),
                    MailPalisadeModel _ => new MailPalisade((MailPalisadeViewModel)vm),
                    _ => (Window)new Palisade((PalisadeViewModel)vm)
                };
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
            if (concrete is CalendarPalisadeModel calendarModel)
            {
                string caldavUrl;
                string username;
                string password;
                if (calendarModel.ZimbraAccountId is Guid accountId && ZimbraAccountStore.GetById(accountId) is ZimbraAccount account)
                {
                    caldavUrl = account.CalDAVBaseUrl ?? string.Empty;
                    username = account.Email ?? string.Empty;
                    password = CredentialEncryptor.Decrypt(account.EncryptedPassword ?? "");
                }
                else
                {
                    caldavUrl = calendarModel.CalDAVBaseUrl ?? string.Empty;
                    username = calendarModel.CalDAVUsername ?? string.Empty;
                    password = CredentialEncryptor.Decrypt(calendarModel.CalDAVPassword ?? "");
                }
                var client = new CalDAVClient(caldavUrl, username, password);
                var calendarService = new CalendarCalDAVService(client);
                return new CalendarPalisadeViewModel(calendarModel, calendarService);
            }
            if (concrete is MailPalisadeModel mailModel)
                return new MailPalisadeViewModel(mailModel);
            if (concrete is StandardPalisadeModel standardModel)
                return new PalisadeViewModel(standardModel);
            return null;
        }

        public static void CreatePalisade(int? x = null, int? y = null, int? width = null, int? height = null)
        {
            var model = new StandardPalisadeModel();
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            if (width.HasValue) model.Width = width.Value;
            if (height.HasValue) model.Height = height.Value;
            var viewModel = new PalisadeViewModel(model);
            palisades.Add(viewModel.Identifier, new Palisade(viewModel));
            viewModel.Save();
        }

        public static void CreateFolderPortal(string rootPath, string title, int? x = null, int? y = null, int? width = null, int? height = null)
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
            var viewModel = new FolderPortalViewModel(model);
            palisades.Add(viewModel.Identifier, new FolderPortal(viewModel));
            viewModel.Save();
        }

        public static void CreateTaskPalisade(string caldavUrl, string username, string password, string taskListId, string title, int? x = null, int? y = null, int? width = null, int? height = null)
        {
            var model = new TaskPalisadeModel
            {
                Name = title,
                CalDAVUrl = caldavUrl,
                CalDAVUsername = username,
                CalDAVPassword = CredentialEncryptor.Encrypt(password),
                TaskListId = taskListId,
                Width = width ?? 600,
                Height = height ?? 400
            };
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            var client = new CalDAVClient(caldavUrl, username, password);
            var caldavService = new Services.CalDAVService(client);
            var viewModel = new TaskPalisadeViewModel(model, caldavService);
            palisades.Add(viewModel.Identifier, new TaskPalisade(viewModel));
            viewModel.Save();
        }

        public static void ShowCreateFolderPortalDialog()
        {
            CreateFolderPortalDialog dialog = new();
            if (dialog.ShowDialog() == true)
            {
                CreateFolderPortal(dialog.SelectedPath, dialog.PortalTitle);
            }
        }

        public static void ShowCreateTaskPalisadeDialog()
        {
            CreateTaskPalisadeDialog dialog = new();
            if (dialog.ShowDialog() == true)
            {
                CreateTaskPalisade(dialog.CalDAVUrl, dialog.Username, dialog.Password, dialog.TaskListId, dialog.PalisadeTitle);
            }
        }

        public static void CreateCalendarPalisade(string caldavUrl, string username, string password, List<string> calendarIds, string title, CalendarViewMode viewMode, int daysToShow, int? x = null, int? y = null, int? width = null, int? height = null)
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
                Height = height ?? 400
            };
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            var client = new CalDAVClient(caldavUrl, username, password);
            var calendarService = new CalendarCalDAVService(client);
            var viewModel = new CalendarPalisadeViewModel(model, calendarService);
            palisades.Add(viewModel.Identifier, new CalendarPalisade(viewModel));
            viewModel.Save();
        }

        public static void ShowCreateCalendarPalisadeDialog()
        {
            var dialog = new CreateCalendarPalisadeDialog();
            try { dialog.Owner = System.Windows.Application.Current.MainWindow; } catch { }
            if (dialog.ShowDialog() != true) return;

            var model = new CalendarPalisadeModel
            {
                Name = dialog.PalisadeTitle,
                CalDAVBaseUrl = dialog.CalDAVUrl,
                CalDAVUsername = dialog.Username,
                CalDAVPassword = CredentialEncryptor.Encrypt(dialog.Password),
                CalendarIds = dialog.SelectedCalendarIds,
                ViewMode = dialog.ViewMode,
                DaysToShow = dialog.DaysToShow,
                Width = 500,
                Height = 400
            };

            var client = new CalDAVClient(model.CalDAVBaseUrl, model.CalDAVUsername, CredentialEncryptor.Decrypt(model.CalDAVPassword));
            var service = new CalendarCalDAVService(client);
            var vm = new CalendarPalisadeViewModel(model, service);
            vm.Save();

            var window = new View.CalendarPalisade(vm);
            palisades[vm.Identifier] = window;
            window.Show();
        }

        public static void CreateMailPalisade(string imapHost, int imapPort, string username, string password, List<string> monitoredFolders, string title, MailDisplayMode displayMode, int pollIntervalMinutes, string? webmailUrl = null, int? x = null, int? y = null, int? width = null, int? height = null)
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
                Height = height ?? 240
            };
            if (x.HasValue) model.FenceX = x.Value;
            if (y.HasValue) model.FenceY = y.Value;
            var viewModel = new MailPalisadeViewModel(model);
            palisades.Add(viewModel.Identifier, new MailPalisade(viewModel));
            viewModel.Save();
        }

        public static void ShowCreateMailPalisadeDialog()
        {
            var dialog = new CreateMailPalisadeDialog();
            if (dialog.ShowDialog() == true)
            {
                CreateMailPalisade(dialog.ImapHost, dialog.ImapPort, dialog.Username, dialog.Password, dialog.SelectedFolders, dialog.PalisadeTitle, dialog.DisplayMode, dialog.PollIntervalMinutes, dialog.WebmailUrl);
            }
        }

        public static void DeletePalisade(string identifier)
        {
            palisades.TryGetValue(identifier, out Window? window);
            if (window == null) return;

            if (window is TabbedPalisade tabbed && tabbed.DataContext is PalisadeGroup group)
            {
                var member = group.Members.FirstOrDefault(m => m.Identifier == identifier);
                if (member == null) return;
                DeleteViewModel(member);
                group.RemoveMember(member);
                foreach (var m in group.Members)
                    palisades.Remove(m.Identifier);
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
                    palisades.Remove(identifier);
                    var standalone = CreateWindowFor(single);
                    palisades[single.Identifier] = standalone;
                }
                else
                {
                    foreach (var m in group.Members)
                        palisades[m.Identifier] = tabbed;
                }
                return;
            }

            if (window.DataContext is PalisadeViewModel palisadeVm)
                palisadeVm.Delete();
            else if (window.DataContext is FolderPortalViewModel folderPortalVm)
                folderPortalVm.Delete();
            else if (window.DataContext is TaskPalisadeViewModel taskPalisadeVm)
                taskPalisadeVm.Delete();
            else if (window.DataContext is CalendarPalisadeViewModel calendarPalisadeVm)
                calendarPalisadeVm.Delete();
            else if (window.DataContext is MailPalisadeViewModel mailPalisadeVm)
                mailPalisadeVm.Delete();

            window.Close();
            palisades.Remove(identifier);
        }

        private static void DeleteViewModel(IPalisadeViewModel vm)
        {
            if (vm is PalisadeViewModel p) p.Delete();
            else if (vm is FolderPortalViewModel f) f.Delete();
            else if (vm is TaskPalisadeViewModel t) t.Delete();
            else if (vm is CalendarPalisadeViewModel c) c.Delete();
            else if (vm is MailPalisadeViewModel m) m.Delete();
        }

        private static Window CreateWindowFor(IPalisadeViewModel vm)
        {
            return vm switch
            {
                PalisadeViewModel p => new Palisade(p),
                FolderPortalViewModel f => new FolderPortal(f),
                TaskPalisadeViewModel t => new TaskPalisade(t),
                CalendarPalisadeViewModel c => new CalendarPalisade(c),
                MailPalisadeViewModel m => new MailPalisade(m),
                _ => throw new NotSupportedException()
            };
        }

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
