using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Utils
{
    // The methods here are rewrites of https://www.mpi-hd.mpg.de/personalhomes/globes/3x3/index.html
    //
    // Joachim Kopp
    // Efficient numerical diagonalization of hermitian 3x3 matrices
    // Int.J.Mod.Phys.C 19 (2008) 523-548
    // arXiv.org: physics/0610206
    public static class Eigs3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Sqr(float x) { return x * x; }

        // Reduces a symmetric 3x3 matrix to tridiagonal form by applying
        // (unitary) Householder transformations:
        //            [ d[0]  e[0]       ]
        //    A = Q . [ e[0]  d[1]  e[1] ] . Q^T
        //            [       e[1]  d[2] ]
        // The function accesses only the diagonal and upper triangular parts of
        // A. The access is read-only.
        public static void MakeTridiagonal(ReadOnlySpan<float> A, Span<float> Q, Span<float> d, Span<float> e)
        {
            const int n = 3;
            Span<float> u = stackalloc float[n];
            Span<float> q = stackalloc float[n];      
            float omega, f;
            float K, h, g;

            // Initialize Q to the identitity matrix

            for (int i = 0; i < n; i++)
            {
                Q[3 * i + i] = 1.0f;
                for (int j = 0; j < i; j++)
                    Q[3 * i + j] = Q[3 * j + i] = 0.0f;
            }

            // Bring first row and column to the desired form 
            h = Sqr(A[1]) + Sqr(A[2]);
            if (A[1] > 0f)
                g = -MathF.Sqrt(h);
            else
                g = MathF.Sqrt(h);
            e[0] = g;
            f = g * A[1];
            u[1] = A[1] - g;
            u[2] = A[2];

            omega = h - f;
            if (omega > 0.0f)
            {
                omega = 1.0f / omega;
                K = 0.0f;
                for (int i = 1; i < n; i++)
                {
                    f = A[3 * 1 + i] * u[1] + A[3 * i + 2] * u[2];
                    q[i] = omega * f;                
                    K += u[i] * f;                 
                }

                K *= 0.5f * Sqr(omega);

                for (int i = 1; i < n; i++)
                    q[i] = q[i] - K * u[i];

                d[0] = A[0];
                d[1] = A[3 * 1 + 1] - 2.0f * q[1] * u[1];
                d[2] = A[3 * 2 + 2] - 2.0f * q[2] * u[2];

                // Store inverse Householder transformation in Q
                for (int j = 1; j < n; j++)
                {
                    f = omega * u[j];
                    for (int i = 1; i < n; i++)
                        Q[3 * i + j] = Q[3 * i + j] - f * u[i];
                }

                // Calculate updated A[3*1 + 2] and store it in e[1]
                e[1] = A[3 * 1 + 2] - q[1] * u[2] - u[1] * q[2];
            }
            else
            {
                for (int i = 0; i < n; i++)
                    d[i] = A[3 * i + i];

                e[1] = A[3 * 1 + 2];
            }
        }

        // Calculates the eigenvalues and normalized eigenvectors of a symmetric 3x3
        // matrix A using the QL algorithm with implicit shifts, preceded by a
        // Householder reduction to tridiagonal form.
        // The function accesses only the diagonal and upper triangular parts of A.
        // The access is read-only.
        public static int DecomposeQL(ReadOnlySpan<float> A, Span<float> Q, Span<float> w)
        {
            const int n = 3;
            Span<float> e = stackalloc float[n];
            float g, r, p, f, b, s, c, t; 
            int nIter;
            int m;

            // Transform A to real tridiagonal form by the Householder method
            MakeTridiagonal(A, Q, w, e);

            // Calculate eigensystem of the remaining real symmetric tridiagonal matrix
            // with the QL method
            //
            // Loop over all off-diagonal elements
            for (int l = 0; l < n - 1; l++)
            {
                nIter = 0;
                while (true)
                {
                    // Check for convergence and exit iteration loop if off-diagonal
                    // element e(l) is zero
                    for (m = l; m <= n - 2; m++)
                    {
                        g = MathF.Sqrt(w[m]) + MathF.Sqrt(w[m + 1]);
                        if (MathF.Sqrt(e[m]) + g == g)
                            break;
                    }

                    if (m == l)
                        break;

                    if (nIter++ >= 30)
                        return -1;

                    // Calculate g = d_m - k
                    g = (w[l + 1] - w[l]) / (e[l] + e[l]);
                    r = MathF.Sqrt(Sqr(g) + 1.0f);
                    if (g > 0)
                        g = w[m] - w[l] + e[l] / (g + r);
                    else
                        g = w[m] - w[l] + e[l] / (g - r);

                    s = c = 1.0f;
                    p = 0.0f;
                    for (int i = m - 1; i >= l; i--)
                    {
                        f = s * e[i];
                        b = c * e[i];
                        if (MathF.Sqrt(f) > MathF.Sqrt(g))
                        {
                            c = g / f;
                            r = MathF.Sqrt(Sqr(c) + 1.0f);
                            e[i + 1] = f * r;
                            c *= (s = 1.0f / r);
                        }
                        else
                        {
                            s = f / g;
                            r = MathF.Sqrt(Sqr(s) + 1.0f);
                            e[i + 1] = g * r;
                            s *= (c = 1.0f / r);
                        }

                        g = w[i + 1] - p;
                        r = (w[i] - g) * s + 2.0f * c * b;
                        p = s * r;
                        w[i + 1] = g + p;
                        g = c * r - b;

                        // Form eigenvectors
                        for (int k = 0; k < n; k++)
                        {
                            t = Q[k * 3 + i + 1];
                            Q[k * 3 + i + 1] = s * Q[k * 3 + i] + c * t;
                            Q[k * 3 + i] = c * Q[k * 3 + i] - s * t;
                        }
                    }
                    w[l] -= p;
                    e[l] = g;
                    e[m] = 0.0f;
                }
            }

            return 0;
        }
    }
}
