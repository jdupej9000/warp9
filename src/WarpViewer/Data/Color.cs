using System;
using System.Numerics;
using System.Threading.Channels;
using Silk.NET.OpenGL;

namespace Warp9.Data
{
    public readonly struct Color
    {
        public Color(uint rgba)
        {
            raw = rgba;
        }

        public Color(int r, int g, int b, int a = 255)
        {
            raw = unchecked((uint)(r | (g << 8) | (b << 16) | (a << 24)));
        }

        readonly uint raw;

        public int R => unchecked((int)(raw & 0xff));
        public int G => unchecked((int)((raw >> 8) & 0xff));
        public int B => unchecked((int)((raw >> 16) & 0xff));
        public int A => unchecked((int)((raw >> 24) & 0xff));

        public uint Raw => raw;

        public Vector4 ToVector()
        {
            return new Vector4(R / 255.0f, G / 255.0f, B / 255.0f, A / 255.0f);
        }

        public Color FromGray(int gray, int a=255)
        {
            return new Color(gray, gray, gray, a);
        }

        public static Color FromVector(Vector4 c)
        { 
            Vector4 cc = Vector4.Multiply(Vector4.Clamp(c, Vector4.Zero, Vector4.One), 255.0f);            
            return new Color((int)cc.X, (int)cc.Y, (int)cc.Z, (int)cc.W);
        }
    }
}