using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Palisades.Helpers;

namespace Palisades.View
{
    public partial class ColorPickerButton : UserControl
    {
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(ColorPickerButton),
                new PropertyMetadata(Colors.White, OnSelectedColorChanged));

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public ColorPickerButton()
        {
            InitializeComponent();
            UpdateBackground();
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPickerButton cpb)
                cpb.UpdateBackground();
        }

        private void UpdateBackground()
        {
            ColorBorder.Background = new SolidColorBrush(SelectedColor);
        }

        private void ColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            using var dlg = new System.Windows.Forms.ColorDialog
                { Color = ColorConversion.ToDrawingColor(SelectedColor), FullOpen = true };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SelectedColor = ColorConversion.ToMediaColor(dlg.Color);
        }
    }
}
