using System.Windows.Media;

namespace Palisades.ViewModel
{
    public interface IPalisadeViewModel
    {
        string Identifier { get; set; }
        string Name { get; set; }
        int FenceX { get; set; }
        int FenceY { get; set; }
        int Width { get; set; }
        int Height { get; set; }
        Color HeaderColor { get; set; }
        Color BodyColor { get; set; }
        SolidColorBrush TitleColor { get; set; }
        SolidColorBrush LabelsColor { get; set; }
        string? GroupId { get; set; }
        int TabOrder { get; set; }
    }
}
