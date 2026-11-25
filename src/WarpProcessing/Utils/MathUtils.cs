using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.Utils
{
    public static class MathUtils
    {
        public static double BetaCf(double a, double b, double x)
        {
            const int MAXIT = 200;
            const double EPS = 3.0e-14;
            const double FPMIN = 1.0e-300;

            double qab = a + b;
            double qap = a + 1.0;
            double qam = a - 1.0;

            double c = 1.0;
            double d = 1.0 - qab * x / qap;
            if (Math.Abs(d) < FPMIN) 
                d = FPMIN;
            
            d = 1.0 / d;
            double h = d;

            for (int m = 1; m <= MAXIT; ++m)
            {
                int m2 = 2 * m;

                double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1.0 + aa * d;
                if (Math.Abs(d) < FPMIN) 
                    d = FPMIN;

                c = 1.0 + aa / c;
                if (Math.Abs(c) < FPMIN) 
                    c = FPMIN;

                d = 1.0 / d;
                h *= d * c;
                aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));

                d = 1.0 + aa * d;
                if (Math.Abs(d) < FPMIN) 
                    d = FPMIN;

                c = 1.0 + aa / c;
                if (Math.Abs(c) < FPMIN) 
                    c = FPMIN;

                d = 1.0 / d;

                double del = d * c;
                h *= del;
                if (Math.Abs(del - 1.0) < EPS)
                    break;
            }

            return h;
        }

        public static double LogGamma(double x)
        {
            // Log of Gamma from Lanczos with g=5, n=6/7
            // not in A & S 
            double[] coef = new double[6] { 76.18009172947146,
                -86.50532032941677, 24.01409824083091,
                -1.231739572450155, 0.1208650973866179E-2,
                -0.5395239384953E-5 };

            double LogSqrtTwoPi = 0.91893853320467274178;
            double denom = x + 1;
            double y = x + 5.5;
            double series = 1.000000000190015;
            for (int i = 0; i < 6; ++i)
            {
                series += coef[i] / denom;
                denom += 1.0;
            }
            return (LogSqrtTwoPi + (x + 0.5) * Math.Log(y) -
                y + Math.Log(series / x));
        }

        public static double RegularizedIncompleteBeta(double a, double b, double x)
        {
            if (x <= 0.0) return 0.0;
            if (x >= 1.0) return 1.0;

            double log_bt = LogGamma(a + b) - LogGamma(a) - LogGamma(b) + 
                    a * Math.Log(x) + b * Math.Log(1 - x);
            double bt = Math.Exp(log_bt);

            if (x < (a + 1.0) / (a + b + 2.0))
                return bt * BetaCf(a, b, x) / a;
            else
                return 1.0 - bt * BetaCf(b, a, 1.0 - x) / b;
        }

        public static bool MeanVariance(ReadOnlySpan<float> x, out double mean, out double var)
        {
            int n = x.Length;
            if (n < 2) 
            {
                mean = 0;
                var = 0;
                return false; 
            }

            double m = 0.0;
            double s = 0.0;


            for (int i = 0; i < n; i++)
            {
                double v = x[i];
                double delta = v - m;
                m += delta / (i + 1);
                s += delta* (v - m);
            }

            mean = m;
            var = s / (n - 1);  // sample variance
            return true;
        }

        public static double FCdf(double f, double d1, double d2)
        {
            if (f <= 0.0) return 0.0;

            double x = (d1 * f) / (d1 * f + d2);
            return RegularizedIncompleteBeta(d1 * 0.5, d2 * 0.5, x);
        }


        public static double WelchTest(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            int na = a.Length;
            int nb = b.Length;

            if (!MeanVariance(a, out double mean_a, out double var_a) ||
                !MeanVariance(b, out double mean_b, out double var_b))
                return double.NaN;
            
            double se2_a = var_a / na;
            double se2_b = var_b / nb;
            double se2 = se2_a + se2_b;

            if (se2 <= 0.0)
                return double.NaN;

            double t = (mean_a - mean_b) / Math.Sqrt(se2);
            double numerator = se2 * se2;
            double denominator = (se2_a * se2_a) / (na - 1) + (se2_b * se2_b) / (nb - 1);

            if (denominator <= 0.0) 
                return double.NaN;

            double df = numerator / denominator;
            double x = df / (df + t * t);
            double p = RegularizedIncompleteBeta(df * 0.5, 0.5, x);

            if (p < 0.0) p = 0.0;
            if (p > 1.0) p = 1.0;

            return p;
        }

        // Galwey (2009) "A new measure of the effective number of
        // tests, a practical tool for comparing families of non-independent
        // significance tests", Genetic Epidemiology 33(7):559-568.
        public static double GalweyEffectiveTests(ReadOnlySpan<float> corEvals, int k)
        {
            double sum_sqrt = 0;
            double sum_lin = 0;

            int kk = Math.Min(k, corEvals.Length);
            for (int i = 0; i < kk; i++)
            {
                double lam = corEvals[i];
                if (lam < 0.0) 
                    lam = 0.0;  

                sum_lin += lam;
                sum_sqrt += Math.Sqrt(lam);
            }

            if (sum_lin <= 0.0) return -1;

            return (sum_sqrt * sum_sqrt) / sum_lin;
        }
    }
}
