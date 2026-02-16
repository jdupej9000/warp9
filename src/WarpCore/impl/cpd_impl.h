#pragma once

#include "../config.h"


namespace warpcore::impl 
{
    int cpd_lowrank_numcols(int m);
    int cpd_tmp_size(int m, int n, int k);
    void cpd_init_clustered(const float* y, int m, int k, int kextra, float beta, float* q, float* lambda);
    float cpd_estimate_sigma(const float* x, const float* y, int m, int n, float* tmp);
    float cpd_estep(const float* x, const float* t, int m, int n, float w, float sigma2, float denom, float* psum, float* pt1, float* p1, float* px);
    float cpd_mstep(const float* y, const float* pt1, const float* p1, const float* px, const float* q, const float* l, const float* linv, int m, int n, int k, float sigma2, float lambda, float* t, float* tmp);
    float cpd_update_sigma2(const float* x, const float* t, const float* pt1, const float* p1, const float* px, int m, int n);

    /*
    template<typename T, size_t NAlign=sizeof(T)>
    class cpdctx
    {
    public:
        cpdctx(void* baseptr, size_t m, size_t n, size_t ne)
        {
            if (baseptr == nullptr) {
                size_t buffer_size = size(m, n, ne);
                _base = (T*)_aligned_malloc(buffer_size, NAlign);
                _is_owner = true;
            } else {
                _base = (T*)baseptr;
                _is_owner = false;
            }
            
            _psum = align(_base);
            _pt1 = align(_psum + n);
            _p1 = align(_pt1 + n);
            _px = align(_p1 + m);
            _tmp = align(_px + 3 * m);
            _ttemp = align(_tmp + cpd_tmp_size((int)m, (int)n, (int)ne));
            _xt = align(_ttemp + 3 * m);
            _yt = align(_ttemp + 3 * n);
            _tt = align(_ttemp + 3 * m);

            _end = _tt + 3 * m;
        }

        ~cpdctx(void)
        {
            if (_is_owner)
                _aligned_free(_base);
        }

    public:
        T* _base;
        T* _psum;
        T* _pt1;
        T* _p1;
        T* _px;
        T* _tmp;
        T* _ttemp;
        T* _xt;
        T* _yt;
        T* _tt;
        T* _end;
        bool _is_owner;

        constexpr size_t size(void) const noexcept 
        {
            return _end - _base;
        }

        static size_t size(size_t m, size_t n, size_t ne)
        {
            cpdctx<T, NAlign> t{ nullptr + 1, m, n, ne };
            return t.size();
        }

    private:
        T* align(T* p) const noexcept 
        {
            void* pp = p & ~(NAlign-1);
            if (pp < p) pp += NAlign;
            return (T*)pp;
        }
    };*/

};
