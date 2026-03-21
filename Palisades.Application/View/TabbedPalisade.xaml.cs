using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Palisades.ViewModel;
using Palisades;
using Palisades.Model;
using Palisades.Services;


namespace Palisades.View
{
    public partial class TabbedPalisade : Window
    {
        private readonly PalisadeGroup _group;

        public TabbedPalisade(PalisadeGroup group)
        {
            InitializeComponent();
            _group = group;
            DataContext = group;

            ApplyTabStyle();
            LocationChanged += (_, _) => SyncBounds();
            SizeChanged += (_, _) => SyncBounds();
            Loaded += (_, _) => SyncBounds();

            PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Delete && e.Key != Key.Back)
                    return;
                if (TabControl.SelectedItem is PalisadeViewModel vm)
                {
                    vm.DeleteShortcut();
                    e.Handled = true;
                }
            };

            Show();
        }

        private void ApplyTabStyle()
        {
            var settings = AppSettingsStore.Load();
            var styleKey = settings.DefaultTabStyle == TabStyle.Rounded ? "RoundedTabItemStyle" : "FlatTabItemStyle";
            if (Resources[styleKey] is System.Windows.Style style)
                TabControl.ItemContainerStyle = style;
        }

        private void SyncBounds()
        {
            if (_group.Members.Count == 0) return;
            _group.SetBounds((int)Left, (int)Top, (int)Width, (int)Height);
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void TitleBarMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (Header.ContextMenu is ContextMenu cm)
            {
                cm.PlacementTarget = sender as UIElement;
                cm.Placement = PlacementMode.Bottom;
                cm.IsOpen = true;
            }
            e.Handled = true;
        }

        private void AddFolderPortalTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement anchor)
                PalisadesManager.RequestAddTab(this, anchor);
            e.Handled = true;
        }

        private void LayoutsSubmenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            if (DataContext is PalisadeGroup g && g.Members.Count > 0 && g.Members[0] is ViewModelBase vb)
                vb.RefreshRecentSnapshots();
        }

        private void TabControl_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;
            if (FindAncestor<TabItem>(e.OriginalSource as DependencyObject) is not TabItem tabItem)
                return;
            if (tabItem.DataContext is not IPalisadeViewModel vm)
                return;

            var menu = new ContextMenu();
            var rename = new MenuItem { Header = "Renommer…" };
            rename.Click += (_, _) => RenameTab(vm);
            menu.Items.Add(rename);

            var left = new MenuItem { Header = "Déplacer à gauche" };
            left.Click += (_, _) => MoveTab(vm, -1);
            left.IsEnabled = _group.Members.IndexOf(vm) > 0;
            menu.Items.Add(left);

            var right = new MenuItem { Header = "Déplacer à droite" };
            right.Click += (_, _) => MoveTab(vm, 1);
            right.IsEnabled = _group.Members.IndexOf(vm) < _group.Members.Count - 1;
            menu.Items.Add(right);

            menu.PlacementTarget = tabItem;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T match)
                    return match;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void RenameTab(IPalisadeViewModel vm)
        {
            var dlg = new RenameSnapshotInputDialog
            {
                Owner = this,
                Title = "Renommer l'onglet",
                PromptLabel = "Nouveau nom",
                CurrentName = vm.Name,
            };
            if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.NewName))
                return;
            vm.Name = dlg.NewName.Trim();
        }

        private void MoveTab(IPalisadeViewModel vm, int delta)
        {
            if (!_group.TryMoveMember(vm, delta))
                return;
            TabControl.SelectedItem = vm;
        }
    }
}
