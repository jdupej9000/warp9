using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Warp9.Data;

namespace Warp9.IO
{
    enum PlyPropertyType
    {
        U8, U16, U32, F32, List, Invalid
    }

    enum PlyPropertySemantic
    {
        PosX, PosY, PosZ, NormX, NormY, NormZ, FaceIndices, None
    }

    enum PlyElementSemantic
    {
        Vertex, Face, None
    }

    class PlyProperty
    {
        public PlyProperty(PlyPropertySemantic sem, PlyPropertyType type, PlyPropertyType count = PlyPropertyType.Invalid, PlyPropertyType elem = PlyPropertyType.Invalid)
        {
            Semantic = sem;
            Type = type;
            CountType = count;
            ElementType = elem;
        }

        public PlyPropertySemantic Semantic { get; }
        public PlyPropertyType Type { get; }
        public PlyPropertyType CountType { get; }
        public PlyPropertyType ElementType { get; }

        static PlyPropertyType ParseType(string t)
        {
            return t switch
            {
                "list" => PlyPropertyType.List,
                "uchar" or "char" => PlyPropertyType.U8,
                "short" or "ushort" => PlyPropertyType.U16,
                "int" or "uint" => PlyPropertyType.U32,
                "float" or "float32" => PlyPropertyType.F32,
                _ => PlyPropertyType.Invalid
            };
        }

        static PlyPropertySemantic ParseSemantic(string t)
        {
            return t switch
            {
                "x" => PlyPropertySemantic.PosX,
                "y" => PlyPropertySemantic.PosY,
                "z" => PlyPropertySemantic.PosZ,
                "nx" => PlyPropertySemantic.NormX,
                "ny" => PlyPropertySemantic.NormY,
                "nz" => PlyPropertySemantic.NormZ,
                "vertex_indices" => PlyPropertySemantic.FaceIndices,
                _ => PlyPropertySemantic.None
            };
        }

        public static PlyProperty? Parse(string[] parts)
        {
            if (parts.Length < 3)
                return null;

            PlyPropertyType t0 = ParseType(parts[1]);
            if (t0 == PlyPropertyType.Invalid)
                return null;

            if (t0 != PlyPropertyType.List)
                return new PlyProperty(ParseSemantic(parts[2]), t0);

            if (parts.Length < 5)
                return null;

            PlyPropertyType t1 = ParseType(parts[2]);
            PlyPropertyType t2 = ParseType(parts[3]);

            if (t1 == PlyPropertyType.List ||
                t1 == PlyPropertyType.F32 ||
                t1 == PlyPropertyType.Invalid ||
                t2 == PlyPropertyType.List ||
                t2 == PlyPropertyType.Invalid)
                return null;

            return new PlyProperty(ParseSemantic(parts[4]), PlyPropertyType.List, t1, t2);
        }
    }

    class PlyElement
    {
        public PlyElement(string name, int numItems)
        {
            Name = name;
            NumItems = numItems;
            Semantic = name switch
            {
                "vertex" => PlyElementSemantic.Vertex,
                "face" => PlyElementSemantic.Face,
                _ => PlyElementSemantic.None
            };
        }

        public string Name { get; }
        public PlyElementSemantic Semantic { get; }
        public int NumItems { get; }
        public List<PlyProperty> Properties { get; } = new List<PlyProperty>();

        public bool TryAddProperty(string[] parts)
        {
            PlyProperty? p = PlyProperty.Parse(parts);
            if (p is null) return false;
            Properties.Add(p);
            return true;
        }

        public bool HasPropertyWithSemantic(PlyPropertySemantic sem) => Properties.Any((t) => t.Semantic == sem);
    }

    public class PlyImport : IDisposable
    {
        private PlyImport(Stream s)
        {
            rd = new BinaryReader(s, Encoding.ASCII);
        }

        BinaryReader rd;
        bool headerValid = false;
        string format = string.Empty;
        List<PlyElement> elements = new List<PlyElement>();

        private string ReadLine()
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                char c = rd.ReadChar();
                if (c == '\n')
                    break;

                sb.Append(c);
            }
            return sb.ToString();
        }

        private bool ParseHeader()
        {
            if (ReadLine().Trim() != "ply")
                return false;

            PlyElement? currentElement = null;

            while (true)
            {
                string? line = ReadLine();
                if (line is null)
                    return false;

                string[] parts = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                    continue;

                switch (parts[0])
                {
                    case "end_header":
                        headerValid = true;
                        return true;

                    case "format":
                        if (parts.Length < 2)
                            return false;
                        else
                            format = parts[1];
                        break;

                    case "comment":
                        break;

                    case "element":
                        if (parts.Length == 3 && int.TryParse(parts[2], out int numItems))
                        {
                            currentElement = new PlyElement(parts[1], numItems);
                            elements.Add(currentElement);
                        }
                        else
                        {
                            return false;
                        }
                        break;

                    case "property":
                        if (!currentElement?.TryAddProperty(parts) ?? false)
                            return false;
                        break;
                }
            }
        }

        private Mesh? ParseMesh()
        {
            if (!headerValid) return null;

            MeshBuilder mb = new MeshBuilder();
            List<Vector3>? pos = null, norm = null;
            List<FaceIndices>? idx = null;

            if (elements.Exists((t) => t.HasPropertyWithSemantic(PlyPropertySemantic.PosX)))
                pos = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Position);

            if (elements.Exists((t) => t.HasPropertyWithSemantic(PlyPropertySemantic.NormX)))
                norm = mb.GetSegmentForEditing<Vector3>(MeshSegmentType.Normal);

            if (elements.Exists((t) => t.HasPropertyWithSemantic(PlyPropertySemantic.FaceIndices)))
                idx = mb.GetIndexSegmentForEditing();

            if (format == "binary_little_endian")
                ParseAsBinaryLittle(pos, norm, idx);
            else
                return null;

            return mb.ToMesh();
        }

        private static float ReadAsFloat(PlyPropertyType type, BinaryReader rd)
        {
            return type switch
            {
                PlyPropertyType.U8 => rd.ReadByte(),
                PlyPropertyType.U16 => rd.ReadUInt16(),
                PlyPropertyType.U32 => rd.ReadUInt32(),
                PlyPropertyType.F32 => rd.ReadSingle(),
                _ => 0
            };
        }

        private static int ReadAsInt(PlyPropertyType type, BinaryReader rd)
        {
            return type switch
            {
                PlyPropertyType.U8 => rd.ReadByte(),
                PlyPropertyType.U16 => rd.ReadUInt16(),
                PlyPropertyType.U32 => (int)rd.ReadUInt32(),
                PlyPropertyType.F32 => (int)rd.ReadSingle(),
                _ => 0
            };
        }

        private static void Skip(PlyPropertyType type, BinaryReader rd)
        {
            switch (type)
            {
                case PlyPropertyType.U8:
                    rd.ReadByte();
                    break;
                case PlyPropertyType.U16:
                    rd.ReadUInt16();
                    break;
                case PlyPropertyType.U32:
                case PlyPropertyType.F32:
                    rd.ReadUInt32();
                    break;
            }
        }

        private static void Skip(PlyPropertyType type, int n, BinaryReader rd)
        {
            switch (type)
            {
                case PlyPropertyType.U8:
                    for (int i = 0; i < n; i++)
                        rd.ReadByte();
                    break;
                case PlyPropertyType.U16:
                    for (int i = 0; i < n; i++)
                        rd.ReadUInt16();
                    break;
                case PlyPropertyType.U32:
                case PlyPropertyType.F32:
                    for (int i = 0; i < n; i++)
                        rd.ReadUInt32();
                    break;
            }
        }

        private void ParseAsBinaryLittle(List<Vector3>? outPos, List<Vector3>? outNorm, List<FaceIndices>? outFace)
        {
            Vector3 pos = Vector3.Zero, normal = Vector3.Zero;
            Span<int> idx = stackalloc int[10];

            foreach (PlyElement element in elements)
            {
                int nprops = element.Properties.Count;
                if (element.Semantic == PlyElementSemantic.Vertex)
                {
                    for (int i = 0; i < element.NumItems; i++)
                    {
                        for (int j = 0; j < nprops; j++)
                        {
                            PlyProperty p = element.Properties[j];
                            if (p.Type == PlyPropertyType.List)
                            {
                                int count = ReadAsInt(p.CountType, rd);
                                Skip(p.ElementType, count, rd);
                            }

                            switch (p.Semantic)
                            {
                                case PlyPropertySemantic.PosX:
                                    pos.X = ReadAsFloat(p.Type, rd);
                                    break;
                                case PlyPropertySemantic.PosY:
                                    pos.Y = ReadAsFloat(p.Type, rd);
                                    break;
                                case PlyPropertySemantic.PosZ:
                                    pos.Z = ReadAsFloat(p.Type, rd);
                                    break;
                                case PlyPropertySemantic.NormX:
                                    normal.X = ReadAsFloat(p.Type, rd);
                                    break;
                                case PlyPropertySemantic.NormY:
                                    normal.Y = ReadAsFloat(p.Type, rd);
                                    break;
                                case PlyPropertySemantic.NormZ:
                                    normal.Z = ReadAsFloat(p.Type, rd);
                                    break;
                                default:
                                    Skip(p.Type, rd);
                                    break;
                            }
                        }

                        if (outPos is not null) outPos.Add(pos);
                        if (outNorm is not null) outNorm.Add(normal);
                    }
                }
                else if (element.Semantic == PlyElementSemantic.Face)
                {
                    for (int i = 0; i < element.NumItems; i++)
                    {
                        for (int j = 0; j < nprops; j++)
                        {
                            PlyProperty p = element.Properties[j];
                            if (p.Type == PlyPropertyType.List)
                            {
                                int count = ReadAsInt(p.CountType, rd);
                                for (int k = 0; k < count; k++)
                                    idx[k] = ReadAsInt(p.ElementType, rd);
                            }
                            else
                            {
                                Skip(p.ElementType, rd);
                            }
                        }

                        if (outFace is not null) outFace.Add(new FaceIndices(idx[0], idx[1], idx[2]));
                    }
                }
            }
        }

        public void Dispose()
        {
            rd.Dispose();
        }

        public static bool TryImport(Stream s, out Mesh m, out string errMsg)
        {
            m = Mesh.Empty;
            errMsg = string.Empty;

            PlyImport import = new PlyImport(s);
            if (!import.ParseHeader())
            {
                errMsg = "PLY header is invalid.";
                return false;
            }

            Mesh? mm = import.ParseMesh();
            if (mm is null)
            {
                errMsg = "Could not read the PLY payload.";
                return false;
            }

            m = mm;
            return true;
        }
    }
}


