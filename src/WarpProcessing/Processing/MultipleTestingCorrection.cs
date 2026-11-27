using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.Processing
{
    public static class MultipleTestingCorrection
    {
        public static void Bonferonni(Span<float> pvals, int numTests = -1)
        {
            float corr = (numTests < 0) ? pvals.Length : numTests;

            for (int i = 0; i < pvals.Length; i++)
                pvals[i] *= corr;
        }

        public static void Sidak(Span<float> pvals, float alpha = 0.05f, int numTests = -1)
        {
            float nt = (numTests < 0) ? pvals.Length : numTests;  
            float thr = 1 - (float)Math.Pow(1 - alpha, 1.0 / nt);
            ApplyThreshold(pvals, thr, 0, 1);
        }

        public static void Holm(Span<float> pvals, float alpha = 0.05f)
        {
            int n = pvals.Length;
            float[] sorted = new float[n];

            pvals.CopyTo(sorted.AsSpan());
            Array.Sort(sorted);

            for (int i = 0; i < n; i++)
            {
                if (sorted[i] > alpha / (i + 1))
                {
                    ApplyThreshold(pvals, sorted[i], 0, 1);
                    return;
                }
            }

            ApplyThreshold(pvals, -1, 0, 1); // set all to not rejected
        }

        public static void BenjaminiHochberg1995(Span<float> pvals, float alpha)
        {
            int n = pvals.Length;
            float[] sorted = new float[n];

            pvals.CopyTo(sorted.AsSpan());
            Array.Sort(sorted);

            for (int i = n - 1; i >= 0; i--)
            {
                if (sorted[i] <= alpha * (i + 1) / n)
                {
                    ApplyThreshold(pvals, sorted[i], 0, 1);
                    return;
                } 
            }

            ApplyThreshold(pvals, -1, 0, 1); // set all to not rejected
        }

        public static void ApplyThreshold(Span<float> x, float threshold, float below, float above)
        {
            for (int i = 0; i < x.Length; i++)
                x[i] = (x[i] >= threshold) ? above : below;
        }
    }
}
