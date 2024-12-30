using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Data
{
    public interface IFaceCollection
    {
        public int FaceCount { get; }
        public bool IsIndexed { get; }
        public bool TryGetIndexData(out ReadOnlySpan<byte> data);
        public MeshView? GetView(MeshViewKind kind, bool cache = true);
        public MeshBuilder ToBuilder();
    }
}
