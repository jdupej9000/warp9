using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Processing
{
    public class DcaVertexRejection
    {
        private DcaVertexRejection(int nv, int n, int[] vertRejections, int[] meshRejections)
        {
            NumVertices = nv;
            NumModels = n;
            VertexRejections = vertRejections;
            MeshRejections = meshRejections;
        }

        public int NumVertices { get; }
        public int NumModels { get; }
        public int[] VertexRejections { get; }
        public int[] MeshRejections { get; }

        public static DcaVertexRejection Create(Mesh baseMesh, IEnumerable<PointCloud> floating, float minTriScale=0.1f, float maxTriScale=10.0f)
        {
            int nv = baseMesh.VertexCount;
            int[] numRejections = new int[nv];
            bool[] currentRejection = new bool[nv];
            List<int> numRejectionsPerMesh = new List<int>();

            MeshView? baseVertView = baseMesh.GetView(MeshViewKind.Pos3f);
            if (baseVertView is null || !baseVertView.AsTypedData(out ReadOnlySpan<Vector3> vertBase))
                throw new InvalidOperationException("Cannot make vertex view in the base mesh.");

            if (!baseMesh.TryGetIndexData(out ReadOnlySpan<FaceIndices> faces))
                throw new InvalidOperationException("Base mesh must be indexed.");

            foreach (PointCloud pcl in floating)
            {
                MeshView? floatVertView = pcl.GetView(MeshViewKind.Pos3f);
                if (floatVertView is null || !floatVertView.AsTypedData(out ReadOnlySpan<Vector3> vertFloat))
                    throw new InvalidOperationException("Cannot make vertex view in a floating mesh.");

                Array.Fill(currentRejection, false);
                ApplyFaceScalingRejection(currentRejection, faces, vertBase, vertFloat, minTriScale, maxTriScale);

                AccumulateRejection(numRejections.AsSpan(), currentRejection.AsSpan());
                numRejectionsPerMesh.Add(currentRejection.Count((t) => t));
            }

            return new DcaVertexRejection(nv, numRejectionsPerMesh.Count, numRejections, numRejectionsPerMesh.ToArray());
        }

        private static void AccumulateRejection(Span<int> numRejections, ReadOnlySpan<bool> currentRejection)
        {
            int nv = numRejections.Length;
            for (int i = 0; i < nv; i++)
            {
                if (currentRejection[i])
                    numRejections[i]++;
            }
        }

        private static void ApplyFaceScalingRejection(Span<bool> reject, ReadOnlySpan<FaceIndices> faces, ReadOnlySpan<Vector3> vertBase, ReadOnlySpan<Vector3> vertFloating, float minTriScale = 0.1f, float maxTriScale = 10.0f)
        {
            for (int i = 0; i < faces.Length; i++)
            {
                FaceIndices f = faces[i];
                float areaBase = MeshUtils.TriangleArea(vertBase[f.I0], vertBase[f.I1], vertBase[f.I2]);
                float areaFloating = MeshUtils.TriangleArea(vertFloating[f.I0], vertFloating[f.I1], vertFloating[f.I2]);

                float r = areaFloating / areaBase;
                if (r < minTriScale || r > maxTriScale)
                {
                    reject[f.I0] = true;
                    reject[f.I1] = true;
                    reject[f.I2] = true;
                }
            }
        }
    }
}
