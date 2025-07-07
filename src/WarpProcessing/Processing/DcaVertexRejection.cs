using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Utils;

namespace Warp9.Processing
{
    public class DcaVertexRejection
    {
        private DcaVertexRejection(int nv, int n, int[] vertRejections, int[] meshRejections, int[] modelMasks)
        {
            NumVertices = nv;
            NumModels = n;
            VertexRejections = vertRejections;
            MeshRejections = meshRejections;
            masks = modelMasks;
        }

        int[] masks;

        public int NumVertices { get; }
        public int NumModels { get; }
        public int[] VertexRejections { get; }
        public int[] MeshRejections { get; }


        public ReadOnlySpan<int> ModelRejectionMask(int idx)
        {
            int nmask = BitMask.GetArraySize(NumVertices);
            return masks.AsSpan().Slice(idx * nmask, nmask);
        }

        public bool[] ToVertexWhitelist(int maxRejectedMeshes)
        {
            int nv = NumVertices;
            bool[] ret = new bool[nv];
            for (int i = 0; i < nv; i++)
                ret[i] = VertexRejections[i] <= maxRejectedMeshes;

            return ret;
        }

        public float[] ToVertexRejectionRates()
        {
            float nrec = 1.0f / NumModels;
            int nv = NumVertices;
            float[] ret = new float[nv];
            for (int i = 0; i < nv; i++)
                ret[i] = (float)VertexRejections[i] * nrec;

            return ret;
        }

        public static DcaVertexRejection Create(Mesh baseMesh, IReadOnlyList<PointCloud> floating, bool rejectExpanded=true, bool rejectDistant=true, float minTriScale=0.1f, float maxTriScale=10.0f, float distFactor=1.5f)
        {
            int nv = baseMesh.VertexCount;
            int[] numRejections = new int[nv];
            bool[] currentRejection = new bool[nv];
            int nmask = BitMask.GetArraySize(nv);
            int[] allMasks = new int[floating.Count * nmask];
            List<int> numRejectionsPerMesh = new List<int>();
            float[] work = new float[nv];

            MeshView? baseVertView = baseMesh.GetView(MeshViewKind.Pos3f);
            if (baseVertView is null || !baseVertView.AsTypedData(out ReadOnlySpan<Vector3> vertBase))
                throw new InvalidOperationException("Cannot make vertex view in the base mesh.");

            if (!baseMesh.TryGetIndexData(out ReadOnlySpan<FaceIndices> faces))
                throw new InvalidOperationException("Base mesh must be indexed.");

            int idx = 0;
            foreach (PointCloud pcl in floating)
            {
                MeshView? floatVertView = pcl.GetView(MeshViewKind.Pos3f);
                if (floatVertView is null || !floatVertView.AsTypedData(out ReadOnlySpan<Vector3> vertFloat))
                    throw new InvalidOperationException("Cannot make vertex view in a floating mesh.");

                Array.Fill(currentRejection, false);

                if(rejectExpanded)
                    ApplyFaceScalingRejection(currentRejection, faces, vertBase, vertFloat, minTriScale, maxTriScale);

                if(rejectDistant)
                    ApplyTranslationRejection(currentRejection, vertBase, vertFloat, work, distFactor);

                AccumulateRejection(numRejections.AsSpan(), currentRejection.AsSpan());
                BitMask.MakeBitMask(allMasks.AsSpan().Slice(idx * nmask, nmask), currentRejection);

                numRejectionsPerMesh.Add(currentRejection.Count((t) => t));
                idx++;
            }

            return new DcaVertexRejection(nv, numRejectionsPerMesh.Count, numRejections, numRejectionsPerMesh.ToArray(), allMasks);
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
                if (float.IsNaN(r) || r < minTriScale || r > maxTriScale)
                {
                    reject[f.I0] = true;
                    reject[f.I1] = true;
                    reject[f.I2] = true;
                }
            }
        }

        private static void ApplyTranslationRejection(Span<bool> reject, ReadOnlySpan<Vector3> vertBase, ReadOnlySpan<Vector3> vertFloating, Span<float> work, float outlierFactor = 1.5f)
        {
            // Assume that the registration resulted in mostly a translation, since the models were
            // prealigned. Calculate the deviation from mean translation. Reject vertices, whose
            // translation distance is outside (q1-factor*iqr, q3+factor*iqr).
            int nv = vertBase.Length;

            double dx = 0, dy = 0, dz = 0;
            for (int i = 0; i < nv; i++)
            {
                Vector3 d = vertFloating[i] - vertBase[i];
                dx += d.X;
                dy += d.Y;
                dz += d.Z;
            }

            Vector3 meanTranslation = new Vector3((float)dx / nv, (float)dy / nv, (float)dz / nv);

            for (int i = 0; i < nv; i++)
                work[i] = Vector3.Distance(meanTranslation, vertFloating[i] - vertBase[i]);

            MemoryExtensions.Sort(work);
            float q1 = work[nv / 4];
            float q3 = work[3 * nv / 4];
            float iqr = q3 - q1;
            float outMin = q1 - outlierFactor * iqr;
            float outMax = q3 + outlierFactor * iqr;

            for (int i = 0; i < nv; i++)
            {
                float d = Vector3.Distance(meanTranslation, vertFloating[i] - vertBase[i]);
                if (d < outMin || d > outMax) 
                    reject[i] = true;
            }
        }
    }
}
