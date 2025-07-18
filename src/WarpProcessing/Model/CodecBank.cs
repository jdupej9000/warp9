﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
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

            ret.Add(ProjectReferenceFormat.PlyMesh, new Codec<Mesh>(
               null, 
               (s, c) =>
               {
                   if (PlyImport.TryImport(s, out Mesh ret, out _))
                       return ret;
                   return null;
               }
           ));

            ret.Add(ProjectReferenceFormat.W9Pcl, new Codec<PointCloud>(
               (s, b, c) => WarpBinExport.ExportPcl(s, b, null),
               (s, c) =>
               {
                   if (WarpBinImport.TryImport(s, out Mesh? pcl))
                       return pcl;
                   return null;
               }
           ));

            ret.Add(ProjectReferenceFormat.W9Mesh, new Codec<Mesh>(
                (s, b, c) => WarpBinExport.ExportMesh(s, b, null),
                (s, c) =>
                {
                    if (WarpBinImport.TryImport(s, out Mesh? m))
                        return m;
                    return null;
                }
            ));

            ret.Add(ProjectReferenceFormat.W9Matrix, new Codec<MatrixCollection>(
               (s, b, c) => WarpBinExport.ExportMatrix(s, b, null),
               (s, c) =>
               {
                   if (WarpBinImport.TryImport(s, out MatrixCollection? m))
                       return m;
                   return null;
               }
            ));

            ret.Add(ProjectReferenceFormat.PngImage, new Codec<Bitmap>(
               (s, b, c) => {
                   using MemoryStream ms = new MemoryStream();
                   b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                   ms.Seek(0, SeekOrigin.Begin);
                   ms.CopyTo(s);
               },
               (s, c) => new Bitmap(Image.FromStream(s))));

            ret.Add(ProjectReferenceFormat.JpegImage, new Codec<Bitmap>(
                (s, b, c) => {
                    using MemoryStream ms = new MemoryStream();
                    b.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.CopyTo(s);
                },
                (s, c) => new Bitmap(Image.FromStream(s))));

            ret.Add(ProjectReferenceFormat.MorphoLandmarks, new Codec<PointCloud>(
                null,
                 (s, c) =>
                 {
                     if (MorphoLandmarkImport.TryImport(s, out PointCloud pcl, out _)) 
                         return pcl;
                     return null;
                 }
            ));

            return ret;
        }
    }

    public class CodecBank<TKey> where TKey : struct
    {
        private readonly Dictionary<TKey, Codec> codecs = new Dictionary<TKey, Codec>();

        public bool TryEncodeObject(Stream dest, object val, TKey type, IEncoderConfig? cfg)
        {
            if (!codecs.TryGetValue(type, out Codec? codec))
                return false;

            return codec.TryEncodeObject(dest, val, cfg);
        }

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
