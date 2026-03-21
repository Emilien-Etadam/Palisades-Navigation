using System.Windows.Input;
using Palisades.ViewModel;

namespace Palisades
{
    public static class PalisadesWpfCommands
    {
        public static ICommand OpenEditSelectedTabCommand { get; } =
            new RelayCommand<IPalisadeViewModel>(vm => PalisadesManager.OpenEditDialog(vm));
    }
}
