namespace Palisades.Model
{
    public class FolderPortalModel : PalisadeModelBase
    {
        private string _rootPath = "";
        private string _currentPath = "";

        public FolderPortalModel()
        {
            Type = PalisadeType.FolderPortal;
        }

        public string RootPath { get => _rootPath; set => _rootPath = value ?? ""; }
        public string CurrentPath { get => _currentPath; set => _currentPath = value ?? ""; }
    }
}
