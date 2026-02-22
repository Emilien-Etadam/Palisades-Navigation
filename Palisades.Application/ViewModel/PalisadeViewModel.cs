using GongSolutions.Wpf.DragDrop;
using Palisades.Helpers;
using Palisades.Model;
using Palisades.View;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Palisades.ViewModel
{
    public class PalisadeViewModel : ViewModelBase, IDropTarget
    {
        private readonly StandardPalisadeModel _model;
        private Shortcut? _selectedShortcut;

        public PalisadeViewModel() : this(new StandardPalisadeModel()) { }

        public PalisadeViewModel(StandardPalisadeModel model) : base(model)
        {
            _model = model;
            Shortcuts.CollectionChanged += (_, _) => Save();
        }

        public ObservableCollection<Shortcut> Shortcuts
        {
            get => _model.Shortcuts;
            set { _model.Shortcuts = value; OnPropertyChanged(); Save(); }
        }

        public Shortcut? SelectedShortcut
        {
            get => _selectedShortcut;
            set { _selectedShortcut = value; OnPropertyChanged(); }
        }

        public ICommand EditPalisadeCommand { get; } = new RelayCommand<PalisadeViewModel>(viewModel =>
        {
            var edit = new EditPalisade
            {
                DataContext = viewModel,
                Owner = PalisadesManager.GetWindow(viewModel.Identifier)
            };
            edit.ShowDialog();
        });

        public void DragOver(IDropInfo dropInfo)
        {
            // Cas 1 : Shortcut (réordonnancement interne ou cross-palissade)
            if (dropInfo.Data is Shortcut)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
                return;
            }

            // Cas 2 : fichiers depuis l'extérieur — tester via dropInfo.Data directement
            if (HasFileDrop(dropInfo))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
                return;
            }

            // Cas 3 : défaut — ne pas accepter (gong montre "sens interdit" si Effects reste None)
            dropInfo.Effects = DragDropEffects.None;
        }

        private static bool HasFileDrop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
                return true;
            if (dropInfo.Data is IDataObject iDataObject && iDataObject.GetDataPresent(DataFormats.FileDrop))
                return true;
            // Gong wraps external drops — check the raw DragEventArgs
            try
            {
                var args = typeof(GongSolutions.Wpf.DragDrop.DropInfo)
                    .GetProperty("DragEventArgs", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(dropInfo) as DragEventArgs;
                if (args?.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    return true;
            }
            catch { }
            return false;
        }

        private static string[]? GetFileDropPaths(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
                return (string[])dataObject.GetData(DataFormats.FileDrop);
            if (dropInfo.Data is IDataObject iDataObject && iDataObject.GetDataPresent(DataFormats.FileDrop))
                return (string[])iDataObject.GetData(DataFormats.FileDrop);
            try
            {
                var args = typeof(GongSolutions.Wpf.DragDrop.DropInfo)
                    .GetProperty("DragEventArgs", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(dropInfo) as DragEventArgs;
                if (args?.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    return (string[])args.Data.GetData(DataFormats.FileDrop);
            }
            catch { }
            return null;
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is Shortcut shortcut)
            {
                if (dropInfo.DragInfo?.SourceCollection is System.Collections.IList sourceList
                    && sourceList != Shortcuts)
                {
                    sourceList.Remove(shortcut);
                }
                else if (dropInfo.DragInfo?.SourceCollection == Shortcuts)
                {
                    Shortcuts.Remove(shortcut);
                }

                int insertIndex = dropInfo.InsertIndex;
                if (insertIndex > Shortcuts.Count)
                    insertIndex = Shortcuts.Count;
                Shortcuts.Insert(insertIndex, shortcut);
                return;
            }

            var files = GetFileDropPaths(dropInfo);
            if (files != null)
            {
                foreach (string file in files)
                {
                    string? extension = Path.GetExtension(file);
                    if (extension == null) continue;
                    Shortcut? shortcutItem = null;
                    if (string.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
                        shortcutItem = LnkShortcut.BuildFrom(file, Identifier);
                    else if (string.Equals(extension, ".url", StringComparison.OrdinalIgnoreCase))
                        shortcutItem = UrlShortcut.BuildFrom(file, Identifier);
                    if (shortcutItem != null)
                        Shortcuts.Add(shortcutItem);
                }
            }
        }

        public ICommand ClickShortcut => new RelayCommand<Shortcut>(SelectShortcut);

        public void SelectShortcut(Shortcut shortcut)
        {
            if (SelectedShortcut == shortcut)
            {
                SelectedShortcut = null;
                return;
            }
            SelectedShortcut = shortcut;
        }

        public ICommand DelKeyPressed => new RelayCommand(DeleteShortcut);

        public void DeleteShortcut()
        {
            if (SelectedShortcut == null) return;
            Shortcuts.Remove(SelectedShortcut);
            SelectedShortcut = null;
        }

    }
}
