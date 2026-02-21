using Palisades.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Palisades
{
    public class PalisadeGroup
    {
        public string GroupId { get; }
        public List<IPalisadeViewModel> Members { get; } = new List<IPalisadeViewModel>();

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
