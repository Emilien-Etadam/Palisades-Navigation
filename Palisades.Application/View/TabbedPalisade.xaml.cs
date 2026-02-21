using System.Windows;
using System.Windows.Input;
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
    }
}
