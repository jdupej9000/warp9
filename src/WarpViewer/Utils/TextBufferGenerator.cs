using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;

namespace Warp9.Utils
{
    public ref struct CharacterRenderInfo
    {
        public Vector2 Pos, Size, TexPos, TexSize;
        public int Index, LineIndex, VisibleIndex;
        public char Character;
    };

    [Flags]
    public enum TextRenderFlags
    {
        AlignLeft = 0,
        AlignCenter = 1,
        AlignRight = 2
    };

    public static class TextBufferGenerator
    {
        public readonly static float XAdvanceScale = 0.8f;

        public static int Generate(FontDefinition font, float size, string text, RectangleF rect, TextRenderFlags flags, Action<CharacterRenderInfo> fun)
        {
            bool mustMeasure = flags.HasFlag(TextRenderFlags.AlignRight) || flags.HasFlag(TextRenderFlags.AlignCenter);

            int lineIndex = 0, visibleIndex = 0;
            float y = rect.Top;
            float lineHeight = font.LineHeight * size;

            foreach (ReadOnlySpan<char> line in new StringLineEnumerator(text.AsSpan()))
            {
                float lineWidth = mustMeasure ? MeasureLineWidth(font, size, line) : 0;

                float x0 = rect.Left;
                if (flags.HasFlag(TextRenderFlags.AlignRight))
                    x0 = rect.Right - lineWidth;
                else if(flags.HasFlag(TextRenderFlags.AlignCenter))
                    x0 = rect.Left + 0.5f * (rect.Width - lineWidth);

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    FontSymbol ch = font.GetSymbol(c);

                    CharacterRenderInfo cri = new CharacterRenderInfo()
                    {
                        Pos = new Vector2(x0 + size * ch.XOffs, y + size * ch.YOffs),
                        Size = new Vector2(size * ch.RealWidth, size * ch.RealHeight),
                        TexPos = new Vector2(ch.X, ch.Y),
                        TexSize = new Vector2(ch.Width, ch.Height),
                        Index = i,
                        VisibleIndex = visibleIndex,
                        LineIndex = lineIndex,
                        Character = c
                    };


                    if (!char.IsWhiteSpace(c))
                    {
                        fun(cri);
                        visibleIndex++;                     
                    }

                    x0 += size * ch.XAdvance * XAdvanceScale;

                    if (i != line.Length - 1)
                        x0 += size * font.Kern(c, line[i + 1]);

                }

                lineIndex++;
                y += lineHeight;
            }

            return visibleIndex;
        }

        public static float MeasureLineWidth(FontDefinition font, float size, ReadOnlySpan<char> line)
        {
            float x = 0;

            char lastChar = '\0';
            for (int i = 0; i < line.Length; i++)
            {
                char thisChar = line[i];

                FontSymbol thisCharDef = font.GetSymbol(thisChar);
                x += thisCharDef.XAdvance;

                x += font.Kern(lastChar, thisChar);
                lastChar = thisChar;                
            }

            return x * size;
        }
    }
}
