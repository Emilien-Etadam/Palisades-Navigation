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
        string? GroupId { get; set; }
        int TabOrder { get; set; }
        void Delete();
        void Save();
    }
}
