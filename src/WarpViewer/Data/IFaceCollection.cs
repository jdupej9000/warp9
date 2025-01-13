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
        public bool TryGetIndexData(out ReadOnlySpan<FaceIndices> data);
        public MeshBuilder ToBuilder();
    }
}
