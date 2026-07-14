using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.Avalonia.Navigation
{
    internal interface IWarp9View
    {
        public void AttachViewModel(Warp9ProjectModel vm);
        public void DetachViewModel();
    }
}
