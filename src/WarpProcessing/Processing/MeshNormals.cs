using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public static class MeshNormals
    {
        public static Mesh MakeNormals(Mesh m)
        {
            MeshView? pos = m.GetView(MeshViewKind.Pos3f);
            if (pos is null || !pos.AsTypedData(out ReadOnlySpan<Vector3> posData))
                throw new InvalidOperationException();

            MeshView? faces = m.GetView(MeshViewKind.Indices3i);
            if (faces is null || !faces.AsTypedData(out ReadOnlySpan<FaceIndices> faceData))
                throw new InvalidOperationException();

            int nv = m.VertexCount;
            int nt = m.FaceCount;
            Vector3[] normal = new Vector3[nv];
            for (int i = 0; i < nv; i++)
                normal[i] = Vector3.Zero;

            for (int i = 0; i < nt; i++)
            {
                FaceIndices f = faceData[i];
                Vector3 a = posData[f.I0];
                Vector3 b = posData[f.I1];
                Vector3 c = posData[f.I2];
                Vector3 n = Vector3.Cross(b - a, c - a);

                normal[f.I0] += n;
                normal[f.I1] += n;
                normal[f.I2] += n;
            }

            MeshBuilder mb = m.ToBuilder();

            List<Vector3> normalsSeg = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Normal);
            for (int i = 0; i < nv; i++)
                normalsSeg.Add(Vector3.Normalize(normal[i]));

            return mb.ToMesh();
        }
    }
}
