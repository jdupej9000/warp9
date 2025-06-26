using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Warp9.Viewer;

namespace Warp9.Data
{
    public class LutSpec
    {
        public LutSpec()
        {
            numSegments = 0;
            numStops = 2;
            stopPos = new float[numStops];
            stopColors = new Color[numStops];

            stopPos[0] = 0;
            stopColors[0] = Color.Black;
            stopPos[1] = 1;
            stopColors[1] = Color.White;
        }

        public LutSpec(int numSeg, (float, Color)[] stops)
        {
            numSegments = numSeg;
            numStops = stops.Length;
            stopPos = new float[numStops];
            stopColors = new Color[numStops];

            for (int i = 0; i < numStops; i++)
            {
                stopPos[i] = stops[i].Item1;
                stopColors[i] = stops[i].Item2;
            }
        }

        public LutSpec(int numSeg, IReadOnlyList<float> pos, IReadOnlyList<Color> col)
        {
            numSegments = numSeg;
            numStops = pos.Count;

            if (col.Count != numStops)
                throw new ArgumentException();

            stopPos = pos.ToArray();
            stopColors = col.ToArray();
        }

        int numStops;
        float[] stopPos;
        Color[] stopColors;
        int numSegments = 0;

        public int NumSegments => numSegments;
        public int NumStops => numStops;
        public float[] StopPos => stopPos;
        public Color[] StopColors => stopColors;

        public bool IsQuantized => numSegments > 0;

        public void SampleRgba8(Span<int> colors)
        {
            if (IsQuantized)
                SampleQuantRgba8(colors);
            else
                SampleSmoothRgba8(colors);
        }

        public Color Sample(float pos)
        {
            if(pos <= 0) return stopColors[0];
            if(pos >= 1) return stopColors[stopColors.Length - 1];

            int idx1 = Math.Abs(Array.BinarySearch(stopPos, pos));
            int idx0 = idx1 - 1;
            if (idx0 < 0) idx0 = 0;
            if(idx1 >= numStops) idx1 = numStops - 1;

            float t = (pos - stopPos[idx0]) / (stopPos[idx1] - stopPos[idx0]);

            Vector4 color0 = RenderUtils.ToNumColor(stopColors[idx0]);
            Vector4 color1 = RenderUtils.ToNumColor(stopColors[idx1]);
            Vector4 colort = Vector4.Lerp(color0, color1, t);

            return RenderUtils.ToColor(colort);
        }

        private void SampleQuantRgba8(Span<int> colors)
        {
            int width = colors.Length;
            for (int i = 0; i < numSegments; i++)
            {
                int j0 = width * i / numSegments;
                int j1 = width * (i + 1) / numSegments;
                if (j1 >= width) j1 = width - 1;

                float segt = ((float)i + 0.5f) / (float)numSegments;

                int segc = Sample(segt).ToArgb();
                for(int j = j0; j < j1; j++) 
                    colors[j] = segc;            
            }
        }

        private void SampleSmoothRgba8(Span<int> colors)
        {
            int width = colors.Length;
            for (int s = 0; s < numStops - 1; s++)
            {
                float f0 = stopPos[s];
                float f1 = stopPos[s + 1];
                Color c0 = stopColors[s];
                Color c1 = stopColors[s + 1];

                int i0 = (int)(width * f0);
                int i1 = (int)(width * f1);

                Vector4 color0 = RenderUtils.ToNumColor(c0);
                Vector4 color1 = RenderUtils.ToNumColor(c1);

                for (int i = i0; i < Math.Min(width, i1); i++)
                {
                    Vector4 color = Vector4.Lerp(color0, color1, (float)(i - i0) / (i1 - i0));
                    colors[i] = RenderUtils.ToColor(color).ToArgb();
                }
            }

            float flast = stopPos[numStops - 1];
            Color clast = stopColors[numStops - 1];
            int ilast = (int)(width * flast);
            if (ilast >= width) ilast = width - 1;
            for (int i = ilast; i < width; i++)
                colors[i] = clast.ToArgb();
        }
    }
}
