#pragma once

#include "../config.h"
#include <Eigen/Core>

namespace warpcore::impl
{
    class ColumnCorrelationOp
    {
    public:
        using Scalar = float;

        ColumnCorrelationOp(const float* p, int d, int n);

        Eigen::Index rows() const { return _n; }
        Eigen::Index cols() const { return _n; }

        
        void perform_op(const Scalar* _xin, Scalar* y_out) const;

    private:
        //using ColMajorMatrixF = Eigen::Matrix<float, Eigen::Dynamic, Eigen::Dynamic, Eigen::ColMajor>;
        Eigen::Map<const Eigen::MatrixXf> _x;
        int _d, _n;
        Eigen::VectorXf _mean, _inv_std;
        std::vector<bool> _degenerate;
    };

};