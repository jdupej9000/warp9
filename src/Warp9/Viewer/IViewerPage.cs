using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Viewer
{
    public interface IViewerPage
    {
        public void SetHist(float[] values, Lut lut, float x0, float x1);
    }
}
