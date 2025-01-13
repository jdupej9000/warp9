using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Warp9.Data;

namespace Warp9.IO
{
    public enum ObjImportMode
    {
        PositionsOnly = 0,
        AllUnshared = 1
    }

    public class ObjImport : IDisposable
    {
        private struct ObjFace
        {
            internal ObjFace(int flags, ReadOnlySpan<int> indices)
            {
                // Somebody, somewhere decided that OBJ indexes vertice base 1. Go figure.
                IdxPos = new FaceIndices(indices[0] - 1, indices[3] - 1, indices[6] - 1);
                IdxTex = new FaceIndices(indices[1] - 1, indices[4] - 1, indices[7] - 1);
                IdxNorm = new FaceIndices(indices[2] - 1, indices[5] - 1, indices[8] - 1);
                FaceType = flags;
            }

            public FaceIndices IdxPos;
            public FaceIndices IdxNorm;
            public FaceIndices IdxTex;
            public int FaceType;

            public const int FaceHasPos = 1;
            public const int FaceHasTex = 2;
            public const int FaceHasNormal = 4;
        }

        private ObjImport(Stream s)
        {
            reader = new StreamReader(s);
        }

        TextReader reader;

        List<Vector3> position = new List<Vector3>();
        List<Vector3> normal = new List<Vector3>();
        List<Vector2> tex0 = new List<Vector2>();
        List<ObjFace> faces = new List<ObjFace>();

        public bool HasError { get; private set; } = false;
        public string ErrorMessage { get; private set; } = string.Empty;

        public void Dispose()
        {
            reader.Dispose();
        }

        private void Parse()
        {
            Span<float> vecf = stackalloc float[4];
            Span<int> veci = stackalloc int[9];
            char vertType;

            int lineCounter = 0;
            string? line;
            while((line = reader.ReadLine()) is not null)
            {
                lineCounter++;

                if (line.Length < 2)
                {
                    continue;
                }
                else if (line.StartsWith('v'))
                {
                    int dim = ParseVertex(line, vecf, out vertType);
                    if (!AddVertex(vecf, vertType, dim))
                    {
                        SetError(lineCounter);
                        break;
                    }
                }
                else if (line.StartsWith('f'))
                {
                    int flags = ParseFace(line, veci);
                    if (flags == 0)
                    {
                        SetError(lineCounter);
                        break;
                    }
                    faces.Add(new ObjFace(flags, veci));
                    
                }
                else if (line.StartsWith('#'))
                {
                }
                else
                {
                    continue;
                }
            }
        }

        private void SetError(int line)
        {
            ErrorMessage = string.Format("Invalid directive at line {0}.", line);
            HasError = true;
        }

        private int ParseVertex(string line, Span<float> vec, out char vertType)
        {
            vertType = line[1];
            int len = line.Length;

            int pos = vertType == ' ' ? 1 : 2;
            return IoUtils.ParseSeparatedFloats(line, pos, ' ', vec);
        }

        private bool AddVertex(Span<float> vec, char vertType, int dim)
        {
            if (vertType == ' ')
            {
                if(dim < 3) 
                    return false;

                position.Add(new Vector3(vec));
                return true;
            }
            else if (vertType == 'n')
            {
                if (dim < 3)
                    return false;

                normal.Add(new Vector3(vec));
                return true;
            }
            else if (vertType == 't')
            {
                if (dim < 2)
                    return false;

                tex0.Add(new Vector2(vec));
                return true;
            }
           
            return false;
        }

        private int ParseFace(string line, Span<int> vec)
        {
            int pos = IoUtils.Skip(line, 1);
            int len = line.Length;
            if (pos == len) 
                return 0;

            int flags = ObjFace.FaceHasPos;

            for (int i = 0; i < 3; i++)
            {
                if (pos == len) 
                    return 0;

                int pos2 = IoUtils.SkipInt(line, pos);
                if(!int.TryParse(line.AsSpan(pos, pos2 - pos), CultureInfo.InvariantCulture, out vec[3 * i]))
                    return 0;

                pos = pos2;
                if (pos == len) break;
                if (line[pos] == ' ') { pos++; continue; }
                pos++; // skip a slash

                pos2 = IoUtils.SkipInt(line, pos);
                if (pos2 != pos)
                {
                    if(!int.TryParse(line.AsSpan(pos, pos2 - pos), CultureInfo.InvariantCulture, out vec[3 * i + 1]))
                        return 0;
                    flags = ObjFace.FaceHasTex;
                    pos = pos2 + 1;
                }

                if (pos < len && line[pos] == '/')
                {
                    pos++;
                    pos2 = IoUtils.SkipInt(line, pos);
                    if (!int.TryParse(line.AsSpan(pos, pos2 - pos), CultureInfo.InvariantCulture, out vec[3 * i + 2]))
                        return 0;

                    flags = ObjFace.FaceHasNormal;
                    pos = pos2;
                }

                pos = IoUtils.Skip(line, pos);
            }

            return flags;
        }

        private Mesh ComposePositionsOnly()
        {
            int nt = faces.Count;

            MeshBuilder builder = new MeshBuilder();
            builder.SetSegment(MeshSegmentType.Position, position);

            List<FaceIndices> fidx = builder.GetIndexSegmentForEditing();
            fidx.Clear();
            fidx.Capacity = nt;
            foreach (ObjFace f in faces)
                fidx.Add(f.IdxPos);

            return builder.ToMesh();
        }

        public Mesh ComposeAllUnshared()
        {
            MeshBuilder builder = new MeshBuilder();

            int nt = faces.Count;

            List<Vector3> pos = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);
            pos.Capacity = nt * 3;
            for (int i = 0; i < nt; i++)
            {
                FaceIndices face = faces[i].IdxPos;
                pos.Add(position[face.I0]);
                pos.Add(position[face.I1]);
                pos.Add(position[face.I2]);
            }

            if (normal.Count > 0)
            {
                List<Vector3> norm = builder.GetSegmentForEditing<Vector3>(MeshSegmentType.Normal);
                norm.Capacity = nt * 3;
                for (int i = 0; i < nt; i++)
                {
                    FaceIndices face = faces[i].IdxNorm;
                    norm.Add(normal[face.I0]);
                    norm.Add(normal[face.I1]);
                    norm.Add(normal[face.I2]);
                }
            }

            if (tex0.Count > 0)
            {
                List<Vector2> t = builder.GetSegmentForEditing<Vector2>(MeshSegmentType.Tex0);
                t.Capacity = nt * 3;
                for (int i = 0; i < nt; i++)
                {
                    FaceIndices face = faces[i].IdxTex;
                    t.Add(tex0[face.I0]);
                    t.Add(tex0[face.I1]);
                    t.Add(tex0[face.I2]);
                }
            }

            return builder.ToMesh();
        }

        public static bool TryImport(Stream s, ObjImportMode mode, out Mesh m, out string errMsg)
        {
            using ObjImport import = new ObjImport(s);
            import.Parse();

            m = mode switch
            {
                ObjImportMode.PositionsOnly => import.ComposePositionsOnly(),
                ObjImportMode.AllUnshared => import.ComposeAllUnshared(),
                _ => throw new InvalidOperationException()
            };

            errMsg = import.ErrorMessage;

            return !import.HasError;
        }       
    }
}
