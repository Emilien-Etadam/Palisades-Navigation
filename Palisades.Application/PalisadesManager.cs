using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using Palisades.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
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

            foreach (PalisadeModelBase concrete in loadedConcrete)
            {
                if (concrete is FolderPortalModel folderModel)
                {
                    var viewModel = new FolderPortalViewModel(folderModel);
                    palisades.Add(concrete.Identifier, new FolderPortal(viewModel));
                }
                else if (concrete is TaskPalisadeModel taskModel)
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
                    var caldavService = new Services.CalDAVService(caldavUrl, username, password);
                    var viewModel = new TaskPalisadeViewModel(taskModel, caldavService);
                    palisades.Add(concrete.Identifier, new TaskPalisade(viewModel));
                }
                else if (concrete is CalendarPalisadeModel calendarModel)
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
                    var calendarService = new CalendarCalDAVService(caldavUrl, username, password);
                    var viewModel = new CalendarPalisadeViewModel(calendarModel, calendarService);
                    palisades.Add(concrete.Identifier, new CalendarPalisade(viewModel));
                }
                else if (concrete is MailPalisadeModel mailModel)
                {
                    var viewModel = new MailPalisadeViewModel(mailModel);
                    palisades.Add(concrete.Identifier, new MailPalisade(viewModel));
                }
                else
                {
                    var standardModel = (StandardPalisadeModel)concrete;
                    palisades.Add(concrete.Identifier, new Palisade(new PalisadeViewModel(standardModel)));
                }
            }
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
            var caldavService = new Services.CalDAVService(caldavUrl, username, password);
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
            var calendarService = new CalendarCalDAVService(caldavUrl, username, password);
            var viewModel = new CalendarPalisadeViewModel(model, calendarService);
            palisades.Add(viewModel.Identifier, new CalendarPalisade(viewModel));
            viewModel.Save();
        }

        public static void ShowCreateCalendarPalisadeDialog()
        {
            var dialog = new CreateCalendarPalisadeDialog();
            if (dialog.ShowDialog() == true)
            {
                CreateCalendarPalisade(dialog.CalDAVUrl, dialog.Username, dialog.Password, dialog.SelectedCalendarIds, dialog.PalisadeTitle, dialog.ViewMode, dialog.DaysToShow);
            }
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
            if (window == null)
            {
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
    }
}
