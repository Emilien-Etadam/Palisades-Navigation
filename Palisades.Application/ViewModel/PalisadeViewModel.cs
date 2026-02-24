using GongSolutions.Wpf.DragDrop;
using Palisades.Helpers;
using Palisades.Model;
using Palisades.View;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

            PasteShortcutCommand = new RelayCommand(() =>
            {
                if (!System.Windows.Clipboard.ContainsFileDropList()) return;
                var files = System.Windows.Clipboard.GetFileDropList();
                if (files == null) return;
                foreach (string? filePath in files)
                {
                    if (string.IsNullOrEmpty(filePath)) continue;
                    var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    var ext = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
                    Shortcut sc = ext == ".url"
                        ? new UrlShortcut { Name = name, UriOrFileAction = filePath, IconPath = filePath }
                        : new LnkShortcut { Name = name, UriOrFileAction = filePath, IconPath = filePath };
                    Shortcuts.Add(sc);
                }
                Save();
            });

            SortByNameCommand = new RelayCommand(() =>
            {
                var sorted = Shortcuts.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
                Shortcuts.Clear();
                foreach (var s in sorted) Shortcuts.Add(s);
                Save();
            });

            RemoveSelectedShortcutCommand = new RelayCommand<Shortcut>(sc =>
            {
                if (sc != null && Shortcuts.Remove(sc)) Save();
            });

            ClickShortcutCommand = new RelayCommand<Shortcut>(SelectShortcut);
            DelKeyPressedCommand = new RelayCommand(DeleteShortcut);
        }

        public ICommand PasteShortcutCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand RemoveSelectedShortcutCommand { get; }
        public ICommand ClickShortcutCommand { get; }
        public ICommand DelKeyPressedCommand { get; }

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

            if (dropInfo.Data is System.Windows.IDataObject dataObject &&
                dataObject.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = System.Windows.DragDropEffects.Copy;
                return;
            }

            dropInfo.Effects = DragDropEffects.None;
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

            if (dropInfo.Data is System.Windows.IDataObject dataObj &&
                dataObj.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var droppedFiles = dataObj.GetData(System.Windows.DataFormats.FileDrop) as string[];
                if (droppedFiles == null) return;
                foreach (var filePath in droppedFiles)
                {
                    var ext = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
                    if (ext == ".lnk" || ext == ".url")
                    {
                        var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        var sc = ext == ".lnk"
                            ? (Shortcut)new LnkShortcut { Name = name, UriOrFileAction = filePath, IconPath = filePath }
                            : new UrlShortcut { Name = name, UriOrFileAction = filePath, IconPath = filePath };
                        Shortcuts.Add(sc);
                    }
                    else
                    {
                        var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        var sc = new LnkShortcut { Name = name, UriOrFileAction = filePath, IconPath = filePath };
                        Shortcuts.Add(sc);
                    }
                }
                Save();
                return;
            }
        }

        public void SelectShortcut(Shortcut shortcut)
        {
            if (SelectedShortcut == shortcut)
            {
                SelectedShortcut = null;
                return;
            }
            SelectedShortcut = shortcut;
        }

        public void DeleteShortcut()
        {
            if (SelectedShortcut == null) return;
            Shortcuts.Remove(SelectedShortcut);
            SelectedShortcut = null;
        }

    }
}
