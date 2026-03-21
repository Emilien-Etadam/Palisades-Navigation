using Palisades.ViewModel;
using System;
using System.Collections.ObjectModel;

namespace Palisades
{
    public class PalisadeGroup
    {
        public string GroupId { get; }
        public ObservableCollection<IPalisadeViewModel> Members { get; } = new ObservableCollection<IPalisadeViewModel>();

        public IPalisadeViewModel? FirstMember => Members.Count > 0 ? Members[0] : null;

        public int X => Members.Count > 0 ? Members[0].FenceX : 0;
        public int Y => Members.Count > 0 ? Members[0].FenceY : 0;
        public int Width => Members.Count > 0 ? Members[0].Width : 800;
        public int Height => Members.Count > 0 ? Members[0].Height : 450;

        public PalisadeGroup(string groupId)
        {
            GroupId = groupId ?? throw new ArgumentNullException(nameof(groupId));
        }

        public void AddMember(IPalisadeViewModel vm)
        {
            if (Members.Count > 0)
            {
                vm.FenceX = X;
                vm.FenceY = Y;
                vm.Width = Width;
                vm.Height = Height;
            }
            vm.GroupId = GroupId;
            vm.TabOrder = Members.Count;
            Members.Add(vm);
        }

        public void RemoveMember(IPalisadeViewModel vm)
        {
            Members.Remove(vm);
            RecalculateTabOrder();
        }

        /// <summary>Déplace un membre d’un cran à gauche (-1) ou à droite (+1) dans la bande d’onglets.</summary>
        public bool TryMoveMember(IPalisadeViewModel vm, int delta)
        {
            int i = Members.IndexOf(vm);
            if (i < 0) return false;
            int newIdx = i + delta;
            if (newIdx < 0 || newIdx >= Members.Count) return false;
            Members.Move(i, newIdx);
            RecalculateTabOrder();
            return true;
        }

        private void RecalculateTabOrder()
        {
            for (int i = 0; i < Members.Count; i++)
                Members[i].TabOrder = i;
        }

        /// <summary>Met à jour position et taille de tous les membres (après déplacement/redimensionnement de la fenêtre groupée).</summary>
        public void SetBounds(int x, int y, int width, int height)
        {
            foreach (var m in Members)
            {
                m.FenceX = x;
                m.FenceY = y;
                m.Width = width;
                m.Height = height;
            }
        }
    }
}
