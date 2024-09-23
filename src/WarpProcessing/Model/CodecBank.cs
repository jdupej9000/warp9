using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.IO;

namespace Warp9.Model
{
    public static class CodecBank
    {
        private static Lazy<CodecBank<ProjectReferenceFormat>> lazyProjectCodecBank = new Lazy<CodecBank<ProjectReferenceFormat>>(
          CreateProjectCodecBank);

        public static CodecBank<ProjectReferenceFormat> ProjectCodecs = lazyProjectCodecBank.Value;

        public static CodecBank<ProjectReferenceFormat> CreateProjectCodecBank()
        {
            CodecBank<ProjectReferenceFormat> ret = new CodecBank<ProjectReferenceFormat>();

            ret.Add(ProjectReferenceFormat.ObjMesh, new Codec<Mesh>(
                null,
                (s, c) =>
                {
                    if (ObjImport.TryImport(s, ObjImportMode.PositionsOnly, out Mesh ret, out _))
                        return ret;
                    return null;
                }
            ));

            ret.Add(ProjectReferenceFormat.W9Mesh, new Codec<Mesh>(
                null,
                null
            ));

            ret.Add(ProjectReferenceFormat.PngImage, new Codec<Bitmap>(
               (s, b, c) => b.Save(s, System.Drawing.Imaging.ImageFormat.Png),
               (s, c) => new Bitmap(Bitmap.FromStream(s))));

            ret.Add(ProjectReferenceFormat.JpegImage, new Codec<Bitmap>(
                (s, b, c) => b.Save(s, System.Drawing.Imaging.ImageFormat.Jpeg),
                (s, c) => new Bitmap(Bitmap.FromStream(s))));

            return ret;
        }
    }

    public class CodecBank<TKey> where TKey : struct
    {
        private readonly Dictionary<TKey, Codec> codecs = new Dictionary<TKey, Codec>();

        public bool TryEncode<T>(Stream dest, T val, TKey type, IEncoderConfig? cfg)
        {
            if (!codecs.TryGetValue(type, out Codec? codec))
                return false;

            return codec.TryEncode(dest, val, cfg);
        }

        public bool TryDecode<T>(Stream src, TKey type, IDecoderConfig? cfg, [MaybeNullWhen(false)] out T val)
        {
            if (!codecs.TryGetValue(type, out Codec? codec))
            {
                val = default;
                return false;
            }

            return codec.TryDecode(src, cfg, out val);
        }

        public void Add(TKey key, Codec c)
        {
            codecs.Add(key, c);
        }
    }
}
