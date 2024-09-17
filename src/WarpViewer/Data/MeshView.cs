using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public enum MeshViewKind
    {
        Pos3f
    }
    
    public class MeshView
    {
        public MeshViewKind Kind { get; private init; }
        public SharpDX.DXGI.Format Format { get; private init; }
        public byte[] RawData {get; private init; }
    }
}
