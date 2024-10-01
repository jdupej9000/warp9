using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.ProjectExplorer
{
    public interface IWarp9View
    {
        public void AttachViewModel(Warp9ViewModel vm);
        public void DetachViewModel();
    }
}
