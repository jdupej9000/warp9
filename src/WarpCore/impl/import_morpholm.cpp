#include "import_utils.h"
#include "utils.h"
#include <vector>
#include <string>
#include <cstring>

namespace warpcore::impl
{
    constexpr int MAX_BUFF = 256;

    struct loadmorpholm_ctx {
        int __magic;
        int flags;

        std::vector<float> v;
        std::vector<std::string> names;

        char incomplete[MAX_BUFF];
        int incomplete_size;
    };
    
    static void process_line(void* ctx, const char* data, int size);
    int tokenize(const char* data, int size, int* offs, int max_offs);

    void* init_morpholmload_ctx(int flags)
    {
        loadmorpholm_ctx* ctx = new loadmorpholm_ctx;
        ctx->__magic = 1;
        ctx->flags = 0;
        ctx->incomplete_size = 0;
        return ctx;
    }

    void deinit_morpholmload_ctx(void* ctx)
    {
        loadmorpholm_ctx* objctx = (loadmorpholm_ctx*)ctx;
        delete objctx;
    }

    int get_pos3fsoa_morpholmload(void* ctx, void* data, int size)
    {
        loadmorpholm_ctx* objctx = (loadmorpholm_ctx*)ctx;
        const int nv = objctx->v.size() / 3;

        if(data && size >= nv) {
            // we're doing this as ints of the same width as optimization
            aos_to_soa<int, 3>((const int*)objctx->v.data(), nv, (int*)data);
        }

        return nv;
    }

    int add_data_morpholmload(void* ctx, const char* data, int size)
    {
        loadmorpholm_ctx* objctx = (loadmorpholm_ctx*)ctx;
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
            process_line(ctx, objctx->incomplete, MAX_BUFF);

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

            process_line(ctx, data + pos, size - pos);
            pos += linelen + 1;
        }

        return 0;
    }

    static void process_line(void* ctx, const char* data, int size)
    {
        loadmorpholm_ctx* objctx = (loadmorpholm_ctx*)ctx;
        int token_start[7];
        int num_tokens = tokenize(data, size, token_start, 7);

        if(num_tokens < 4) return;

        objctx->names.push_back(std::string{
            data + token_start[0], 
            (size_t)find_first(data + token_start[0], size - token_start[0], '\t')});

        for(int i = 0; i < 3; i++)
            objctx->v.push_back((float) atof(data + token_start[i + 1]));
    }

    int tokenize(const char* data, int size, int* offs, int max_offs)
    {
        if(size == 0) return 0;

        int pos = 0;
        int token = 0;

        while(token < max_offs && pos < size) {
            int num_sep = skip_seq(data + pos, size - pos, '\t');
            if(data[pos + num_sep] == '\t') break;

            offs[token++] = pos + num_sep;
            pos += num_sep;
            int num_field = find_first(data + pos, size - pos, '\t');
            if(num_field == -1) break;

            pos += num_field;
        }

        return token;
    }
};