#include "eigs.h"
#include <cmath>
#include <Eigen/Core>
#include <Spectra/SymEigsSolver.h>

using namespace std;
using namespace Eigen;
using namespace Spectra;

namespace warpcore::impl
{
    ColumnCorrelationOp::ColumnCorrelationOp(const float* p, int d, int n) :
        _x(Eigen::Map<const Eigen::MatrixXf>(p, d, n)),
        _d(d), _n(n),
        _mean(n),
        _inv_std(n),
        _degenerate(n, false)
    {
        if (d < 2) throw invalid_argument("Need at least 2 rows.");
        if (n < 1) throw invalid_argument("Need at least 1 column.");

        _mean = _x.rowwise().mean();   /* O(m*n) */

        constexpr float eps = 1e-20f;

        for (int i = 0; i < _d; i++) {
            float ss = 0.0f;
            for (int j = 0; j < _n; j++) {
                float dd = _x(i, j) - _mean(i);
                ss += dd * dd;
            }
            float var = ss / (float)(_n - 1);
            if (var > eps) {
                _inv_std(i) = 1.0f / std::sqrt(var);
                _degenerate[i] = false;
            }
            else {
                _inv_std(i) = 0.0f;
                _degenerate[i] = true;      /* guard zero-variance rows */
            }
        }
    }

    void ColumnCorrelationOp::perform_op(const Scalar* xin, Scalar* yout) const
    {
        Map<const VectorXf> v(xin, _d);

        VectorXf w = v.cwiseProduct(_inv_std);          /* D^{-1/2} v, d-vector */
        VectorXf Xtw = _x.transpose() * w;              /* O(dN) GEMV, N-vector */
        float mean_dot_w = _mean.dot(w);
        VectorXf u = Xtw.array() - mean_dot_w;          /* Xc^T * w */

        VectorXf Xu = _x * u;                           /* O(dN) GEMV, d-vector */
        float sum_u = u.sum();
        VectorXf z = Xu - _mean * sum_u;                /* Xc * u */

        float inv_N1 = 1.0f / (float)(_n - 1);
        for (int i = 0; i < _d; i++) {
            yout[i] = _degenerate[i] ? xin[i]
                : z(i) * _inv_std(i) * inv_N1;
        }
    }
};