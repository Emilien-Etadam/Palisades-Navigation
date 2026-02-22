using GongSolutions.Wpf.DragDrop;
using Palisades.Helpers;
using Palisades.Model;
using Palisades.View;
using System;
using System.Collections.ObjectModel;
using System.IO;
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
            if (dropInfo.Data is Shortcut)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = System.Windows.DragDropEffects.Move;
                return;
            }

            if (dropInfo.Data is System.Windows.IDataObject dataObject
                && dataObject.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = System.Windows.DragDropEffects.Copy;
                return;
            }

            if (dropInfo.Data is System.Windows.DataObject wpfDataObject
                && wpfDataObject.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = System.Windows.DragDropEffects.Copy;
                return;
            }
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

            string[]? files = null;
            if (dropInfo.Data is System.Windows.IDataObject dataObject
                && dataObject.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                files = (string[])dataObject.GetData(System.Windows.DataFormats.FileDrop);
            }
            else if (dropInfo.Data is System.Windows.DataObject wpfDataObject
                && wpfDataObject.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                files = (string[])wpfDataObject.GetData(System.Windows.DataFormats.FileDrop);
            }

            if (files != null)
            {
                foreach (string file in files)
                {
                    string? extension = System.IO.Path.GetExtension(file);
                    if (extension == null) continue;

                    Shortcut? shortcutItem = null;
                    if (extension == ".lnk")
                        shortcutItem = LnkShortcut.BuildFrom(file, Identifier);
                    else if (extension == ".url")
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
