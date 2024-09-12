#include "import_obj.h"
#include "utils.h"
#include "import_utils.h"
#include <string.h>
#include <cstdlib>

namespace warpcore::impl
{
    constexpr int MAX_BUFF = 256;

    struct loadobj_ctx {
        int __magic;
        int flags;

        std::vector<float> v;
        std::vector<float> vt;
        std::vector<float> vn;
        std::vector<int> f;

        char incomplete[MAX_BUFF];
        int incomplete_size;
    };

    enum LINE_TYPE {
        LTY_POS = 0,
        LTY_TEX,
        LTY_NORM,
        LTY_FACE,
        LTY_IGNORE,
        LTY_INVALID,
        LTY_END
    };

    static int process_line(void* ctx, const char* data, int size);
    void pushvert(std::vector<float>& d, int stride, int parsed, const float* s);
    void pushface(std::vector<int>& d, int stride, const int* s);
    int parse_floats(const char* data, const int* ts, float* bf);
    int parse_ints(const char* data, const int* ts, int* bi);
    void tokenize_v(const char* data, int start, int size, int* offs);
    void tokenize_f(const char* data, int size, int* offs);
    int tokenize_line(const char* data, int size, int* offs);

    void* init_objload_ctx(int flags)
    {
        loadobj_ctx* ctx = new loadobj_ctx;
        ctx->__magic = 0;
        ctx->flags = 0;
        ctx->incomplete_size = 0;
        return ctx;
    }

    void deinit_objload_ctx(void* ctx)
    {
        loadobj_ctx* objctx = (loadobj_ctx*)ctx;
        delete objctx;
    }

    int get_pos3fsoa_objload(void* ctx, void* data, int size)
    {
        loadobj_ctx* objctx = (loadobj_ctx*)ctx;
        const int nv = objctx->v.size() / 3;

        if(data && size >= nv) {
            // we're doing this as ints of the same width as optimization
            aos_to_soa<int, 3>((const int*)objctx->v.data(), nv, (int*)data);
        }

        return nv;
    }

    int get_idx3isoa_objload(void* ctx, void* data, int size)
    {
        loadobj_ctx* objctx = (loadobj_ctx*)ctx;
        const int nf = objctx->f.size() / 9;

         if(data && size >= nf) {
            aos_to_soa<int, 3, 3>((const int*)objctx->f.data(), nf, (int*)data);
        }

        return nf;
    }

    int add_data_objload(void* ctx, const char* data, int size)
    {
        loadobj_ctx* objctx = (loadobj_ctx*)ctx;
        bool is_last = size == 0;

        int pos = 0;
        if(objctx->incomplete_size) {
            // assemble the buffer using the new data
            const int comp = std::min(size, MAX_BUFF - objctx->incomplete_size);
            memcpy(objctx->incomplete + objctx->incomplete_size, data, comp);
            
            // find the line ending, request more data if that cannot be found
            int linelen = find_line_end(objctx->incomplete, objctx->incomplete_size + comp);
            if(linelen < 0 && is_last) {
                linelen = objctx->incomplete_size + comp;
            }
            
            if(linelen < 0 && objctx->incomplete_size < MAX_BUFF) {
                objctx->incomplete_size += comp;
                return 0; // ask once more to refill the buffer
            } else if(linelen < 0) {
                return -1; // ERROR: the line cannot fit into the buffer
            }

            // process the assembled line
            const int line_type = process_line(ctx, objctx->incomplete, MAX_BUFF);
            if(line_type == LTY_INVALID)
                return -1;
            
            // advance the pointer
            pos = linelen - objctx->incomplete_size;
            objctx->incomplete_size = 0;
        }

        while(pos < size) {
            int linelen = find_line_end(data + pos, size - pos);
            if(linelen < 0) {
                int incompl_size = size - pos;
                if(incompl_size > MAX_BUFF)
                    return -1; // ERROR: not enough data to hold incomplete end

                // back up the unprocessed remainder and return
                memcpy(objctx->incomplete, data + pos, incompl_size);
                objctx->incomplete_size = incompl_size;
                return 0; // incomplete line, must wait for more data
            }

            int line_type = process_line(ctx, data + pos, size - pos);
            //if(line_type == LTY_INVALID)
            //    return -1;

            pos += linelen + 1;
        }

        return 0;
    }

    static int process_line(void* ctx, const char* data, int size)
    {
        loadobj_ctx* objctx = (loadobj_ctx*)ctx;

        int tokens[10];
        float bf[4];
        int bi[10];
        int parse_result = 0;
        int line_type = tokenize_line(data, size, tokens);

        switch(line_type) {
        case LTY_POS:
            parse_result = parse_floats(data, tokens, bf);
            pushvert(objctx->v, 3, parse_result, bf);
            break; 

        case LTY_TEX:
            parse_result = parse_floats(data, tokens, bf);
            pushvert(objctx->vt, 2, parse_result, bf);
            break; 

        case LTY_NORM:
            parse_result = parse_floats(data, tokens, bf);
            pushvert(objctx->vn, 3, parse_result, bf);
            break; 

        case LTY_FACE:
            parse_result = parse_ints(data, tokens, bi);
            pushface(objctx->f, 9, bi);
            break;
        }

        return line_type;
    }

    void pushvert(std::vector<float>& d, int stride, int parsed, const float* s)
    {
        int k = std::min(stride, parsed);
        for(int i = 0; i < k; i++)
            d.push_back(s[i]);

        for(int i = k; k < stride; i++)
            d.push_back(0);
    }

    void pushface(std::vector<int>& d, int stride, const int* s)
    {
        for(int i = 0; i < stride; i++)
            d.push_back(s[i] - 1);
    }

    int parse_floats(const char* data, const int* ts, float* bf)
    {
        for(int i = 0; i < 3; i++) {
            if(ts[i] >= 0)
                bf[i] = atof(data + ts[i]);
        }
        return 3;
    }

    int parse_ints(const char* data, const int* ts, int* bi)
    {
        for(int i = 0; i < 9; i++) {
            if(ts[i] >= 0)
                bi[i] = atoi(data + ts[i]);
            else
                bi[i] = -1;
        }

        return 9;
    }


    void tokenize_v(const char* data, int start, int size, int* offs)
    {
        int i = start;
        offs[0] = i;
        i += skip_float(data + i, size - i) + 1;
        offs[1] = i;
        i += skip_float(data + i, size - i) + 1;
        offs[2] = i;
    }

    void tokenize_f(const char* data, int size, int* offs)
    {
        int i = 2;

        for(int vert = 0; vert < 3; vert++) {
            int seg = 0;
            for(seg = 0; seg < 3; seg++) {
                const int int_len = skip_int(data + i, size - i);
                offs[3 * vert + seg] = int_len > 0 ? i : -1;

                i += int_len;
                const char sep = data[i];
                i++;

                if(sep== ' ' || sep == '\t')
                    break;
            }

            for(seg++; seg < 3; seg++) {
                offs[3 * vert + seg] = -1;
            }
        }
    }

    int tokenize_line(const char* data, int size, int* offs)
    {
        if(size == 0)
            return LTY_END;

        const char d0 = data[0];
        const char d1 = data[1];

        int ret = LTY_INVALID;
        switch(d0) {
        case 'v':
            switch(d1) {
            case 't': ret = LTY_TEX; break;
            case 'n': ret = LTY_NORM; break;
            default: ret = LTY_POS; break;
            }
            tokenize_v(data, ret == LTY_POS ? 2 : 3, size, offs);
            break;

        case 'f':
            tokenize_f(data, size, offs);
            ret = LTY_FACE;
            break;

        case '#':
            ret = LTY_IGNORE;
            break;
        }

        return ret;
    }
};
