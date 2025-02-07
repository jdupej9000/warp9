using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Utils;

namespace Warp9.Data
{
    public class FontSymbol(float X, float Y, float Width, float Height, float XOffs, float YOffs, float XAdvance, int Page = 0, int Channel = 0);

    public class FontDefinition
    {
        private Dictionary<char, FontSymbol> symbols = new Dictionary<char, FontSymbol>();
        private Dictionary<int, float> kerning = new Dictionary<int, float>();

        public string FaceName { get; private set; } = string.Empty;
        public float FontSize { get; private set; } = -1;
        public float LineHeight { get; private set; } = -1;
        public float BaseY { get; private set; } = -1;
        public string BitmapFileName { get; private set; } = string.Empty;
        public int BitmapWidth { get; private set; } = -1;
        public int BitmapHeight { get; private set; } = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MakePairHash(char a, char b)
        {
            return (int)a | ((int)b << 16);
        }

        public static FontDefinition FromStream(Stream s)
        {
            using StreamReader sr = new StreamReader(s);

            FontDefinition ret = new FontDefinition();
            while (true)
            {
                string? line = sr.ReadLine();
                if (line is null)
                    break;

                KeyValueLineParser parser = new KeyValueLineParser(line.AsSpan());
                if (!parser.TryGetNextToken(out ReadOnlySpan<char> directive, out _))
                    continue;

                bool lineOk = directive switch
                {
                    "info" => ParseInfo(ret, parser),
                    "common" => ParseCommon(ret, parser),
                    "page" => ParsePage(ret, parser),
                    "chars" => true,
                    "char" => ParseChar(ret, parser),
                    "kernings" => true,
                    "kerning" => ParseKerning(ret, parser),
                    _ => false
                };

                if (!lineOk)
                    throw new InvalidOperationException("Error parsing line: " + line);
            }

            return ret;
        }

        private static bool ParseInfo(FontDefinition def, KeyValueLineParser parser)
        {
            // info face="Segoe UI" size=32 bold=0 italic=0 charset="" unicode=0 stretchH=100 smooth=1 aa=1 padding=4,4,4,4 spacing=-2,-2
            while (parser.TryGetNextToken(out ReadOnlySpan<char> key, out ReadOnlySpan<char> value))
            {
                switch (key)
                {
                    case "face":
                        def.FaceName = new string(value);
                        break;

                    case "size" when int.TryParse(value, CultureInfo.InvariantCulture, out int fontSize):
                        def.FontSize = fontSize;
                        break;

                }
            }

            return true;
        }

        private static bool ParseCommon(FontDefinition def, KeyValueLineParser parser)
        {
            // common lineHeight=49 base=35 scaleW=512 scaleH=512 pages=3 packed=0
            while (parser.TryGetNextToken(out ReadOnlySpan<char> key, out ReadOnlySpan<char> value))
            {
                switch (key)
                {
                    case "lineHeight" when int.TryParse(value, CultureInfo.InvariantCulture, out int lineHeight):
                        def.LineHeight = lineHeight;
                        break;

                    case "base" when int.TryParse(value, CultureInfo.InvariantCulture, out int baseY):
                        def.BaseY = baseY;
                        break;

                    case "scaleW" when int.TryParse(value, CultureInfo.InvariantCulture, out int bmpWidth):
                        def.BitmapWidth = bmpWidth;
                        break;

                    case "scaleH" when int.TryParse(value, CultureInfo.InvariantCulture, out int bmpHeight):
                        def.BitmapHeight = bmpHeight;
                        break;

                    case "pages":
                    case "packed":
                        break;

                    default:
                        return false;
                }
            }
            return true;
        }

        private static bool ParsePage(FontDefinition def, KeyValueLineParser parser)
        {
            // page id=0 file="segoe1.png"
            while (parser.TryGetNextToken(out ReadOnlySpan<char> key, out ReadOnlySpan<char> value))
            {
                switch (key)
                {
                    case "file":
                        def.BitmapFileName = new string(value);
                        break;

                    case "id" when int.TryParse(value, CultureInfo.InvariantCulture, out int pageIndex) && pageIndex != 0:
                    default:
                        return false;
                }
            }
            return true;
        }

        private static bool ParseChar(FontDefinition def, KeyValueLineParser parser)
        {
            if(def.BitmapWidth <= 0 || def.BitmapHeight <= 0 || def.LineHeight <=0)
                return false;

            float lineHeightR = 1.0f / def.LineHeight;
            float bmpWidthR = 1.0f / def.BitmapWidth;
            float bmpHeightR = 1.0f / def.BitmapHeight;

            // char id=13      x=0    y=0    width=0    height=0    xoffset=-4   yoffset=0    xadvance=6    page=0    chnl=0

            int id = -1, x = -1, y = -1, width = -1, height = -1, xoffset = 0, yoffset = 0, xadvance = -1, page = 0, chnl = 0;
            while (parser.TryGetNextToken(out ReadOnlySpan<char> key, out ReadOnlySpan<char> value))
            {
                switch (key)
                {   
                    case "id" when int.TryParse(value, CultureInfo.InvariantCulture, out id):
                        break;

                    case "x" when int.TryParse(value, CultureInfo.InvariantCulture, out x):
                        break;

                    case "y" when int.TryParse(value, CultureInfo.InvariantCulture, out y):
                        break;

                    case "width" when int.TryParse(value, CultureInfo.InvariantCulture, out width):
                        break;

                    case "height" when int.TryParse(value, CultureInfo.InvariantCulture, out height):
                        break;

                    case "xoffset" when int.TryParse(value, CultureInfo.InvariantCulture, out xoffset):
                        break;

                    case "yoffset" when int.TryParse(value, CultureInfo.InvariantCulture, out yoffset):
                        break;

                    case "xadvance" when int.TryParse(value, CultureInfo.InvariantCulture, out xadvance):
                        break;

                    case "page" when int.TryParse(value, CultureInfo.InvariantCulture, out page):
                        break;

                    case "chnl" when int.TryParse(value, CultureInfo.InvariantCulture, out chnl):
                        break;

                    default:
                        return false;
                }
            }

            if (id <= 0 || x < 0 || y < 0 || width < 0 || height < 0 || xadvance < 0 || page != 0 || chnl != 0)
                return false;

            def.symbols.Add((char)id, new FontSymbol(
                x * bmpWidthR, y * bmpHeightR, 
                width * bmpWidthR, height * bmpHeightR, 
                xoffset * lineHeightR, yoffset * lineHeightR, 
                xadvance * lineHeightR, 
                page, chnl));

            return true;
        }

        private static bool ParseKerning(FontDefinition def, KeyValueLineParser parser)
        {
            if (def.LineHeight <= 0)
                return false;

            float lineHeightR = 1.0f / def.LineHeight;

            // kerning first=290 second=121 amount=-1

            int first = -1, second = -1, amount = 0;
            while (parser.TryGetNextToken(out ReadOnlySpan<char> key, out ReadOnlySpan<char> value))
            {
                switch (key)
                {
                    case "first" when int.TryParse(value, CultureInfo.InvariantCulture, out first):
                        break;

                    case "second" when int.TryParse(value, CultureInfo.InvariantCulture, out second):
                        break;

                    case "amount" when int.TryParse(value, CultureInfo.InvariantCulture, out amount):
                        break;

                    default:
                        return false;
                }
            }

            if (first <= 0 || second <= 0)
                return false;

            def.kerning.Add(MakePairHash((char)first, (char)second), amount * lineHeightR);

            return true;
        }
    }

}
