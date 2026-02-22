using GongSolutions.Wpf.DragDrop;
using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Palisades.ViewModel
{
    public class FolderPortalViewModel : INotifyPropertyChanged, IPalisadeViewModel, IDropTarget, IDragSource
    {
        #region Attributes
        private readonly FolderPortalModel model;
        private volatile bool shouldSave;
        private readonly object _saveLock = new object();
        private readonly System.Threading.Timer _saveTimer;
        private ObservableCollection<FolderPortalItem> items;
        private string breadcrumb;
        private string currentFolderName;
        private string errorMessage;
        #endregion

        #region Accessors
        public string Identifier
        {
            get { return model.Identifier; }
            set { model.Identifier = value; OnPropertyChanged(); Save(); }
        }

        public string Name
        {
            get { return model.Name; }
            set { model.Name = value; OnPropertyChanged(); Save(); }
        }

        public int FenceX
        {
            get { return model.FenceX; }
            set { model.FenceX = value; OnPropertyChanged(); Save(); }
        }

        public int FenceY
        {
            get { return model.FenceY; }
            set { model.FenceY = value; OnPropertyChanged(); Save(); }
        }

        public int Width
        {
            get { return model.Width; }
            set { model.Width = value; OnPropertyChanged(); Save(); }
        }

        public int Height
        {
            get { return model.Height; }
            set { model.Height = value; OnPropertyChanged(); Save(); }
        }

        public Color HeaderColor
        {
            get { return model.HeaderColor; }
            set { model.HeaderColor = value; OnPropertyChanged(); Save(); }
        }

        public Color BodyColor
        {
            get { return model.BodyColor; }
            set { model.BodyColor = value; OnPropertyChanged(); Save(); }
        }

        public SolidColorBrush TitleColor
        {
            get => new(model.TitleColor);
            set { model.TitleColor = value.Color; OnPropertyChanged(); Save(); }
        }

        public SolidColorBrush LabelsColor
        {
            get => new(model.LabelsColor);
            set { model.LabelsColor = value.Color; OnPropertyChanged(); Save(); }
        }

        public string? GroupId { get => model.GroupId; set { model.GroupId = value; OnPropertyChanged(); Save(); } }
        public int TabOrder { get => model.TabOrder; set { model.TabOrder = value; OnPropertyChanged(); Save(); } }

        public string RootPath
        {
            get { return model.RootPath; }
            set { model.RootPath = value; OnPropertyChanged(); Save(); }
        }

        public string CurrentPath
        {
            get { return model.CurrentPath; }
            set
            {
                model.CurrentPath = value;
                OnPropertyChanged();
                UpdateBreadcrumb();
                Save();
            }
        }

        public ObservableCollection<FolderPortalItem> Items
        {
            get { return items; }
            set { items = value; OnPropertyChanged(); }
        }

        public string Breadcrumb
        {
            get { return breadcrumb; }
            set { breadcrumb = value; OnPropertyChanged(); }
        }

        public string CurrentFolderName
        {
            get { return currentFolderName; }
            set { currentFolderName = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; OnPropertyChanged(); }
        }

        public bool CanNavigateBack
        {
            get
            {
                if (string.IsNullOrEmpty(RootPath) || string.IsNullOrEmpty(CurrentPath))
                    return false;
                return !string.Equals(
                    Path.GetFullPath(CurrentPath).TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
            }
        }
        #endregion

        public FolderPortalViewModel() : this(new FolderPortalModel { Name = "Folder Portal" })
        { }

        public FolderPortalViewModel(FolderPortalModel model)
        {
            this.model = model;
            items = new ObservableCollection<FolderPortalItem>();
            breadcrumb = "";
            currentFolderName = "";
            errorMessage = "";

            if (!string.IsNullOrEmpty(model.CurrentPath) && Directory.Exists(model.CurrentPath))
            {
                LoadFolder(model.CurrentPath);
            }
            else if (!string.IsNullOrEmpty(model.RootPath) && Directory.Exists(model.RootPath))
            {
                model.CurrentPath = model.RootPath;
                LoadFolder(model.RootPath);
            }

            UpdateBreadcrumb();

            _saveTimer = new System.Threading.Timer(_ => SaveAsync(), null, 1000, 1000);
        }

        #region Methods
        public void Save()
        {
            shouldSave = true;
        }

        public void Delete()
        {
            string saveDirectory = PDirectory.GetPalisadeDirectory(Identifier);
            if (Directory.Exists(saveDirectory))
            {
                Directory.Delete(Path.Combine(saveDirectory), true);
            }
        }

        public void LoadFolder(string path)
        {
            ErrorMessage = "";

            if (!Directory.Exists(path))
            {
                ErrorMessage = "Folder not found: " + path;
                Items.Clear();
                return;
            }

            try
            {
                var newItems = new ObservableCollection<FolderPortalItem>();

                // Load subdirectories first (skip hidden)
                foreach (string dir in Directory.GetDirectories(path).OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
                {
                    if (IsHiddenOrSystemEntry(dir))
                        continue;
                    string dirName = Path.GetFileName(dir);
                    string iconPath = GetOrCreateFolderIcon(dir);
                    newItems.Add(new FolderPortalItem(dirName, dir, true, iconPath));
                }

                // Then load files (skip hidden and temp files like ~$*)
                foreach (string file in Directory.GetFiles(path).OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.StartsWith("~$") || IsHiddenOrSystemEntry(file))
                        continue;
                    string iconPath = GetOrCreateFileIcon(file);
                    newItems.Add(new FolderPortalItem(fileName, file, false, iconPath));
                }

                Items = newItems;
                CurrentPath = path;
                OnPropertyChanged(nameof(CanNavigateBack));
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "Access denied: " + path;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error: " + ex.Message;
            }
        }

        private void UpdateBreadcrumb()
        {
            if (string.IsNullOrEmpty(RootPath) || string.IsNullOrEmpty(CurrentPath))
            {
                Breadcrumb = "";
                CurrentFolderName = "";
                return;
            }

            string rootFull = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar);
            string currentFull = Path.GetFullPath(CurrentPath).TrimEnd(Path.DirectorySeparatorChar);

            CurrentFolderName = Path.GetFileName(currentFull);
            if (string.IsNullOrEmpty(CurrentFolderName))
            {
                CurrentFolderName = currentFull; // Drive root like C:\
            }

            // Build breadcrumb relative to root
            string rootName = Path.GetFileName(rootFull);
            if (string.IsNullOrEmpty(rootName))
            {
                rootName = rootFull; // Drive root
            }

            if (string.Equals(rootFull, currentFull, StringComparison.OrdinalIgnoreCase))
            {
                Breadcrumb = rootName;
            }
            else
            {
                string relativePath = currentFull.Substring(rootFull.Length).TrimStart(Path.DirectorySeparatorChar);
                string[] parts = relativePath.Split(Path.DirectorySeparatorChar);
                Breadcrumb = rootName + " > " + string.Join(" > ", parts);
            }
        }

        private static bool IsHiddenOrSystemEntry(string path)
        {
            try
            {
                FileAttributes attrs = File.GetAttributes(path);
                return (attrs & FileAttributes.Hidden) != 0 || (attrs & FileAttributes.System) != 0;
            }
            catch
            {
                return false;
            }
        }

        private string GetOrCreateFolderIcon(string folderPath)
        {
            string iconsDir = PDirectory.GetPalisadeIconsDirectory(Identifier);
            PDirectory.EnsureExists(iconsDir);

            // Use a stable hash-based name for folder icons to avoid creating duplicates
            string hashName = "folder_" + folderPath.GetHashCode().ToString("X") + ".png";
            string iconPath = Path.Combine(iconsDir, hashName);

            if (File.Exists(iconPath))
                return iconPath;

            try
            {
                using Bitmap? icon = IconExtractor.GetFileImageFromPath(folderPath, Helpers.Native.IconSizeEnum.LargeIcon48);
                if (icon != null)
                {
                    using FileStream fileStream = new(iconPath, FileMode.Create);
                    icon.Save(fileStream, ImageFormat.Png);
                    return iconPath;
                }
            }
            catch
            {
                // Fall through to return empty string
            }

            return "";
        }

        private string GetOrCreateFileIcon(string filePath)
        {
            string iconsDir = PDirectory.GetPalisadeIconsDirectory(Identifier);
            PDirectory.EnsureExists(iconsDir);

            string hashName = "file_" + filePath.GetHashCode().ToString("X") + ".png";
            string iconPath = Path.Combine(iconsDir, hashName);

            if (File.Exists(iconPath))
                return iconPath;

            try
            {
                using Bitmap? icon = IconExtractor.GetFileImageFromPath(filePath, Helpers.Native.IconSizeEnum.LargeIcon48);
                if (icon != null)
                {
                    using FileStream fileStream = new(iconPath, FileMode.Create);
                    icon.Save(fileStream, ImageFormat.Png);
                    return iconPath;
                }
            }
            catch
            {
                // Fall through to return empty string
            }

            return "";
        }

        private void RefreshItems()
        {
            if (!string.IsNullOrEmpty(CurrentPath))
                LoadFolder(CurrentPath);
        }
        #endregion

        #region IDragSource
        public void StartDrag(IDragInfo dragInfo)
        {
            if (dragInfo.SourceItem is FolderPortalItem item && !string.IsNullOrEmpty(item.FullPath))
            {
                var dataObject = new DataObject(DataFormats.FileDrop, new[] { item.FullPath });
                dragInfo.DataObject = dataObject;
                dragInfo.Effects = DragDropEffects.Copy | DragDropEffects.Move;
            }
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            return dragInfo.SourceItem is FolderPortalItem;
        }

        public void Dropped(IDropInfo dropInfo)
        {
        }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
            if (operationResult.HasFlag(DragDropEffects.Move))
                RefreshItems();
        }

        public void DragCancelled()
        {
        }

        public bool TryCatchOccurredException(Exception exception)
        {
            return false;
        }
        #endregion

        #region IDropTarget
        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject dataObject
                && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                bool isCopy = (dropInfo.KeyStates & DragDropKeyStates.ControlKey) != 0;
                dropInfo.Effects = isCopy ? DragDropEffects.Copy : DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                return;
            }

            if (dropInfo.Data is IDataObject iDataObject
                && iDataObject.GetDataPresent(DataFormats.FileDrop))
            {
                bool isCopy = (dropInfo.KeyStates & DragDropKeyStates.ControlKey) != 0;
                dropInfo.Effects = isCopy ? DragDropEffects.Copy : DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                return;
            }

            if (dropInfo.Data is FolderPortalItem)
            {
                bool isCopy = (dropInfo.KeyStates & DragDropKeyStates.ControlKey) != 0;
                dropInfo.Effects = isCopy ? DragDropEffects.Copy : DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                return;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            string targetDir = CurrentPath;
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
                return;

            string[]? files = null;

            if (dropInfo.Data is DataObject dataObject
                && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                files = (string[])dataObject.GetData(DataFormats.FileDrop);
            }
            else if (dropInfo.Data is IDataObject iDataObject
                && iDataObject.GetDataPresent(DataFormats.FileDrop))
            {
                files = (string[])iDataObject.GetData(DataFormats.FileDrop);
            }
            else if (dropInfo.Data is FolderPortalItem item && !string.IsNullOrEmpty(item.FullPath))
            {
                files = new[] { item.FullPath };
            }

            if (files == null || files.Length == 0)
                return;

            bool isCopy = (dropInfo.KeyStates & DragDropKeyStates.ControlKey) != 0;

            foreach (string sourcePath in files)
            {
                try
                {
                    string fileName = Path.GetFileName(sourcePath);
                    string destPath = Path.Combine(targetDir, fileName);

                    if (string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (File.Exists(destPath) || Directory.Exists(destPath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        int counter = 1;
                        do
                        {
                            destPath = Path.Combine(targetDir, $"{nameWithoutExt} ({counter}){ext}");
                            counter++;
                        }
                        while (File.Exists(destPath) || Directory.Exists(destPath));
                    }

                    if (Directory.Exists(sourcePath))
                    {
                        if (isCopy)
                            CopyDirectory(sourcePath, destPath);
                        else
                            Directory.Move(sourcePath, destPath);
                    }
                    else if (File.Exists(sourcePath))
                    {
                        if (isCopy)
                            File.Copy(sourcePath, destPath);
                        else
                            File.Move(sourcePath, destPath);
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Failed to {(isCopy ? "copy" : "move")} {Path.GetFileName(sourcePath)}: {ex.Message}";
                }
            }

            RefreshItems();
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
            foreach (string dir in Directory.GetDirectories(sourceDir))
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
        #endregion

        #region Commands
        public ICommand NavigateIntoFolderCommand
        {
            get
            {
                return new RelayCommand<FolderPortalItem>((item) =>
                {
                    if (item == null) return;

                    if (item.IsDirectory)
                    {
                        LoadFolder(item.FullPath);
                    }
                    else
                    {
                        // Open files with default application
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = item.FullPath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            ErrorMessage = "Cannot open file: " + ex.Message;
                        }
                    }
                });
            }
        }

        public ICommand NavigateBackCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (!CanNavigateBack) return;

                    string? parent = Directory.GetParent(CurrentPath)?.FullName;
                    if (parent != null)
                    {
                        // Don't navigate above the root
                        string rootFull = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar);
                        string parentFull = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar);

                        if (parentFull.Length >= rootFull.Length)
                        {
                            LoadFolder(parent);
                        }
                    }
                });
            }
        }

        public ICommand OpenInExplorerCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (string.IsNullOrEmpty(CurrentPath) || !Directory.Exists(CurrentPath))
                        return;

                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = CurrentPath,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // Silently ignore if explorer fails
                    }
                });
            }
        }

        public ICommand RefreshCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (!string.IsNullOrEmpty(CurrentPath))
                    {
                        LoadFolder(CurrentPath);
                    }
                });
            }
        }

        public ObservableCollection<LayoutSnapshot> RecentSnapshots { get; } = new();

        public void RefreshRecentSnapshots()
        {
            RecentSnapshots.Clear();
            foreach (var s in LayoutSnapshotService.ListSnapshots().Take(5))
                RecentSnapshots.Add(s);
        }

        public ICommand RestoreSnapshotCommand { get; } = new RelayCommand<string>(id =>
        {
            if (string.IsNullOrEmpty(id)) return;
            if (System.Windows.MessageBox.Show("This will replace your current layout. Continue?", "Restore layout",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;
            LayoutSnapshotService.RestoreSnapshot(id);
        });

        public ICommand NavigateToRootCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (!string.IsNullOrEmpty(RootPath) && Directory.Exists(RootPath))
                    {
                        LoadFolder(RootPath);
                    }
                });
            }
        }

        public ICommand NewPalisadeCommand { get; private set; } = new RelayCommand(() =>
        {
            PalisadesManager.CreatePalisade();
        });

        public ICommand NewFolderPortalCommand { get; private set; } = new RelayCommand(() =>
        {
            PalisadesManager.ShowCreateFolderPortalDialog();
        });

        public ICommand NewTaskPalisadeCommand { get; private set; } = new RelayCommand(() => PalisadesManager.ShowCreateTaskPalisadeDialog());
        public ICommand NewCalendarPalisadeCommand { get; private set; } = new RelayCommand(() => PalisadesManager.ShowCreateCalendarPalisadeDialog());
        public ICommand NewMailPalisadeCommand { get; private set; } = new RelayCommand(() => PalisadesManager.ShowCreateMailPalisadeDialog());
        public ICommand ManageZimbraAccountsCommand { get; private set; } = new RelayCommand(() => { var d = new View.ManageAccountsDialog(); d.ShowDialog(); });

        public ICommand SaveSnapshotCommand { get; private set; } = new RelayCommand(() =>
        {
            var dialog = new View.SaveSnapshotDialog();
            try { dialog.Owner = Application.Current.MainWindow; } catch { }
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SnapshotName))
                LayoutSnapshotService.SaveSnapshot(dialog.SnapshotName.Trim());
        });

        public ICommand ManageSnapshotsCommand { get; private set; } = new RelayCommand(() =>
        {
            var dialog = new View.ManageSnapshotsDialog();
            try { dialog.Owner = Application.Current.MainWindow; } catch { }
            dialog.ShowDialog();
        });

        public ICommand DeletePalisadeCommand { get; private set; } = new RelayCommand<string>((identifier) => PalisadesManager.DeletePalisade(identifier));

        public ICommand EditFolderPortalCommand { get; private set; } = new RelayCommand<FolderPortalViewModel>((viewModel) =>
        {
            EditFolderPortal edit = new()
            {
                DataContext = viewModel
            };
            try
            {
                edit.Owner = PalisadesManager.GetWindow(viewModel.Identifier);
            }
            catch { }
            edit.ShowDialog();
        });

        private void SaveAsync()
        {
            if (!shouldSave) return;
            lock (_saveLock)
            {
                if (!shouldSave) return;
                try
                {
                    string saveDirectory = PDirectory.GetPalisadeDirectory(Identifier);
                    PDirectory.EnsureExists(saveDirectory);
                    using StreamWriter writer = new(Path.Combine(saveDirectory, "state.xml"));
                    ViewModelBase.SharedSerializer.Serialize(writer, model);
                }
                catch { /* réessayer au prochain cycle */ }
                finally { shouldSave = false; }
            }
        }
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
