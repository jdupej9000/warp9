using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;

namespace Warp9.ProjectExplorer
{
    public class ProjExpModel
    {
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<ProjExpModel> Children { get; set; } = new ObservableCollection<ProjExpModel>();
    }

}
