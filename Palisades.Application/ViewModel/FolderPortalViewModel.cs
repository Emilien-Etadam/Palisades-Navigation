using GongSolutions.Wpf.DragDrop;
using Palisades.Helpers;
using Palisades.Model;
using Palisades.Services;
using Palisades.View;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Palisades.ViewModel
{
    public class FolderPortalViewModel : ViewModelBase, IDropTarget, IDragSource
    {
        private readonly FolderPortalModel _model;
        private ObservableCollection<FolderPortalItem> _items;
        private string _breadcrumb;
        private string _currentFolderName;
        private string _errorMessage;

        public string RootPath
        {
            get => _model.RootPath;
            set { _model.RootPath = value; OnPropertyChanged(); Save(); }
        }

        public string CurrentPath
        {
            get => _model.CurrentPath;
            set
            {
                _model.CurrentPath = value;
                OnPropertyChanged();
                UpdateBreadcrumb();
                Save();
            }
        }

        public ObservableCollection<FolderPortalItem> Items
        {
            get => _items;
            set { _items = value; OnPropertyChanged(); }
        }

        public string Breadcrumb
        {
            get => _breadcrumb;
            set { _breadcrumb = value; OnPropertyChanged(); }
        }

        public string CurrentFolderName
        {
            get => _currentFolderName;
            set { _currentFolderName = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
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

        public FolderPortalViewModel() : this(new FolderPortalModel { Name = "Folder Portal" })
        { }

        public FolderPortalViewModel(FolderPortalModel model) : base(model)
        {
            _model = model;
            _items = new ObservableCollection<FolderPortalItem>();
            _breadcrumb = "";
            _currentFolderName = "";
            _errorMessage = "";

            if (!string.IsNullOrEmpty(model.CurrentPath) && Directory.Exists(model.CurrentPath))
                LoadFolder(model.CurrentPath);
            else if (!string.IsNullOrEmpty(model.RootPath) && Directory.Exists(model.RootPath))
            {
                model.CurrentPath = model.RootPath;
                LoadFolder(model.RootPath);
            }

            UpdateBreadcrumb();

            CreateNewFolderCommand = new RelayCommand(() =>
            {
                var currentPath = GetCurrentDirectoryPath();
                if (string.IsNullOrEmpty(currentPath)) return;
                var name = "New Folder";
                var path = Path.Combine(currentPath, name);
                var counter = 1;
                while (Directory.Exists(path))
                {
                    name = $"New Folder ({counter++})";
                    path = Path.Combine(currentPath, name);
                }
                Directory.CreateDirectory(path);
                RefreshCommand.Execute(null);
            });

            CreateNewFileCommand = new RelayCommand(() =>
            {
                var currentPath = GetCurrentDirectoryPath();
                if (string.IsNullOrEmpty(currentPath)) return;
                var name = "New File.txt";
                var path = Path.Combine(currentPath, name);
                var counter = 1;
                while (File.Exists(path))
                {
                    name = $"New File ({counter++}).txt";
                    path = Path.Combine(currentPath, name);
                }
                File.WriteAllText(path, string.Empty);
                RefreshCommand.Execute(null);
            });

            PasteFromClipboardCommand = new RelayCommand(() =>
            {
                var currentPath = GetCurrentDirectoryPath();
                if (string.IsNullOrEmpty(currentPath)) return;
                if (!Clipboard.ContainsFileDropList()) return;
                var files = Clipboard.GetFileDropList();
                if (files == null) return;
                foreach (string? source in files)
                {
                    if (string.IsNullOrEmpty(source)) continue;
                    var destName = Path.GetFileName(source);
                    var dest = Path.Combine(currentPath, destName);
                    try
                    {
                        if (File.Exists(source))
                            File.Copy(source, dest, false);
                        else if (Directory.Exists(source))
                            CopyDirectory(source, dest);
                    }
                    catch { }
                }
                RefreshCommand.Execute(null);
            });

            NavigateIntoFolderCommand = new RelayCommand<FolderPortalItem>(item =>
            {
                if (item == null) return;
                if (item.IsDirectory)
                    LoadFolder(item.FullPath);
                else
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = item.FullPath, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = "Cannot open file: " + ex.Message;
                    }
                }
            });

            NavigateBackCommand = new RelayCommand(() =>
            {
                if (!CanNavigateBack) return;
                string? parent = Directory.GetParent(CurrentPath)?.FullName;
                if (parent != null)
                {
                    string rootFull = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar);
                    string parentFull = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar);
                    if (parentFull.Length >= rootFull.Length)
                        LoadFolder(parent);
                }
            });

            OpenInExplorerCommand = new RelayCommand(() =>
            {
                if (string.IsNullOrEmpty(CurrentPath) || !Directory.Exists(CurrentPath)) return;
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = CurrentPath, UseShellExecute = true });
                }
                catch { }
            });

            RefreshCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(CurrentPath))
                    LoadFolder(CurrentPath);
            });

            NavigateToRootCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(RootPath) && Directory.Exists(RootPath))
                    LoadFolder(RootPath);
            });

            EditFolderPortalCommand = new RelayCommand<FolderPortalViewModel>(viewModel =>
            {
                var edit = new EditFolderPortal { DataContext = viewModel };
                try { edit.Owner = PalisadesManager.GetWindow(viewModel.Identifier); } catch { }
                edit.ShowDialog();
            });
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

                foreach (string dir in Directory.GetDirectories(path).OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
                {
                    if (IsHiddenOrSystemEntry(dir))
                        continue;
                    string dirName = Path.GetFileName(dir);
                    string iconPath = GetOrCreateFolderIcon(dir);
                    newItems.Add(new FolderPortalItem(dirName, dir, true, iconPath));
                }

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
                CurrentFolderName = currentFull;

            string rootName = Path.GetFileName(rootFull);
            if (string.IsNullOrEmpty(rootName))
                rootName = rootFull;

            if (string.Equals(rootFull, currentFull, StringComparison.OrdinalIgnoreCase))
                Breadcrumb = rootName;
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
                var attrs = File.GetAttributes(path);
                return (attrs & FileAttributes.Hidden) != 0 || (attrs & FileAttributes.System) != 0;
            }
            catch { return false; }
        }

        private static string StableHash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash, 0, 4);
        }

        private string GetOrCreateFolderIcon(string folderPath)
        {
            string iconsDir = PDirectory.GetPalisadeIconsDirectory(Identifier);
            PDirectory.EnsureExists(iconsDir);
            string hashName = "folder_" + StableHash(folderPath) + ".png";
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
            catch { }
            return "";
        }

        private string GetOrCreateFileIcon(string filePath)
        {
            string iconsDir = PDirectory.GetPalisadeIconsDirectory(Identifier);
            PDirectory.EnsureExists(iconsDir);
            string hashName = "file_" + StableHash(filePath) + ".png";
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
            catch { }
            return "";
        }

        private void RefreshItems()
        {
            if (!string.IsNullOrEmpty(CurrentPath))
                LoadFolder(CurrentPath);
        }

        private string GetCurrentDirectoryPath()
        {
            return CurrentPath ?? "";
        }

        #region IDragSource
        public void StartDrag(IDragInfo dragInfo)
        {
            if (dragInfo.SourceItem is FolderPortalItem item && !string.IsNullOrEmpty(item.FullPath))
            {
                dragInfo.DataObject = new DataObject(DataFormats.FileDrop, new[] { item.FullPath });
                dragInfo.Effects = DragDropEffects.Copy | DragDropEffects.Move;
            }
        }

        public bool CanStartDrag(IDragInfo dragInfo) => dragInfo.SourceItem is FolderPortalItem;

        public void Dropped(IDropInfo dropInfo) { }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
            if (operationResult.HasFlag(DragDropEffects.Move))
                RefreshItems();
        }

        public void DragCancelled() { }

        public bool TryCatchOccurredException(Exception exception) => false;
        #endregion

        #region IDropTarget
        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                bool isCopy = (dropInfo.KeyStates & DragDropKeyStates.ControlKey) != 0;
                dropInfo.Effects = isCopy ? DragDropEffects.Copy : DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                return;
            }
            if (dropInfo.Data is IDataObject iDataObject && iDataObject.GetDataPresent(DataFormats.FileDrop))
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
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            string[]? files = null;
            if (dropInfo.Data is DataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
                files = (string[])dataObject.GetData(DataFormats.FileDrop);
            else if (dropInfo.Data is IDataObject iDataObject && iDataObject.GetDataPresent(DataFormats.FileDrop))
                files = (string[])iDataObject.GetData(DataFormats.FileDrop);
            else if (dropInfo.Data is FolderPortalItem item && !string.IsNullOrEmpty(item.FullPath))
                files = new[] { item.FullPath };

            if (files == null || files.Length == 0)
                return;

            bool isCopy = (dropInfo.KeyStates & DragDropKeyStates.ControlKey) != 0;
            ImportFileSystemPaths(files, isCopy);
        }

        public void ImportExplorerFileDrop(string[] files, bool isCopy)
        {
            if (files == null || files.Length == 0)
                return;
            ImportFileSystemPaths(files, isCopy);
        }

        private void ImportFileSystemPaths(string[] files, bool isCopy)
        {
            string targetDir = CurrentPath;
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
                return;

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
        public ICommand CreateNewFolderCommand { get; }
        public ICommand CreateNewFileCommand { get; }
        public ICommand PasteFromClipboardCommand { get; }
        public ICommand NavigateIntoFolderCommand { get; }
        public ICommand NavigateBackCommand { get; }
        public ICommand OpenInExplorerCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand NavigateToRootCommand { get; }
        public ICommand EditFolderPortalCommand { get; }
        #endregion
    }
}
