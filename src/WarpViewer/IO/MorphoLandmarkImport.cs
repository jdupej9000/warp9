using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Warp9.Data;

namespace Warp9.IO
{
    public class MorphoLandmarkImport : IDisposable
    {
        private MorphoLandmarkImport(Stream s)
        {
            reader = new StreamReader(s);
        }

        StreamReader reader;
        string? errorMessage = null;
        List<float> positionData = new List<float>();
        int commonVectorSize = -1;

        private PointCloud Compose()
        {
            MeshBuilder builder = new MeshBuilder();

            if (positionData.Count % commonVectorSize != 0)
                throw new InvalidOperationException();

            if (commonVectorSize == 3)
            {
                List<Vector3> vec = builder.GetSegmentForEditing<Vector3>(MeshSegmentSemantic.Position);
                for (int i = 0; i < positionData.Count; i += 3)
                    vec.Add(new Vector3(positionData[i], positionData[i + 1], positionData[i + 2]));
            }
            else if (commonVectorSize == 2)
            {
                List<Vector2> vec = builder.GetSegmentForEditing<Vector2>(MeshSegmentSemantic.Position);
                for (int i = 0; i < positionData.Count; i += 2)
                    vec.Add(new Vector2(positionData[i], positionData[i + 1]));
            }
            else
            {
                throw new InvalidOperationException();
            }

            return builder.ToPointCloud();
        }

        private void Parse()
        {
            Span<float> parsedBuffer = stackalloc float[3];
        
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                int pos = IoUtils.SkipAllBut(line, 0, '\t') + 1;
                if (pos >= line.Length)
                    continue;

                int vectorSize = IoUtils.ParseSeparatedFloats(line, pos, '\t', parsedBuffer);

                if (commonVectorSize == -1)
                {
                    commonVectorSize = vectorSize;
                }
                else if (commonVectorSize != vectorSize)
                {
                    errorMessage = "Jagged arrays are not allowed.";
                    break;
                }

                for (int i = 0; i < commonVectorSize; i++)
                    positionData.Add(parsedBuffer[i]);
            }
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        public static bool TryImport(Stream s, out PointCloud pcl, out string errMsg)
        {
            using MorphoLandmarkImport import = new MorphoLandmarkImport(s);
            import.Parse();
            pcl = import.Compose();
            errMsg = import.errorMessage ?? string.Empty;
            return import.errorMessage is null;
        }
    }
}
