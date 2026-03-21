using System;
using System.Windows;
using Palisades.View;
using Palisades.ViewModel;

namespace Palisades
{
    /// <summary>Crée la fenêtre WPF associée à un ViewModel de palisade (extrait de <see cref="PalisadesManager"/>).</summary>
    internal static class PalisadeWindowFactory
    {
        public static Window Create(IPalisadeViewModel vm) => vm switch
        {
            PalisadeViewModel p => new Palisade(p),
            FolderPortalViewModel f => new FolderPortal(f),
            TaskPalisadeViewModel t => new TaskPalisade(t),
            CalendarPalisadeViewModel c => new CalendarPalisade(c),
            MailPalisadeViewModel m => new MailPalisade(m),
            _ => throw new NotSupportedException("No window for " + vm.GetType().Name)
        };
    }
}
