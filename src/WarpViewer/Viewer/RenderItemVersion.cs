using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Viewer
{
    [Flags]
    public enum RenderItemDelta
    {
        None = 0,
        Dynamic = 1,
        Full = 0x7fffffff
    };

    public class RenderItemVersion
    {
        public RenderItemVersion()
        {
            Full = 0;
            Dynamic = 0;
        }

        public RenderItemVersion(uint full, uint dyn = 0)
        {
            Full = full;
            Dynamic = dyn;
        }

        public ulong Full { get; private set; }
        public ulong Dynamic { get; private set; }

        public void Commit(RenderItemDelta delta)
        {
            if(delta == RenderItemDelta.None)
                return;

            ulong ddyn = delta.HasFlag(RenderItemDelta.Dynamic) ? 1U : 0;
            ulong dfull = delta == RenderItemDelta.Full ? 1U : 0;

            Full = unchecked(Full + dfull);
            Dynamic = unchecked(Dynamic + ddyn);
        }

        public RenderItemDelta Upgrade(RenderItemVersion to)
        {
            RenderItemDelta d = Compare(this, to);

            if (d.HasFlag(RenderItemDelta.Dynamic))
                Dynamic = to.Dynamic;

            if (d == RenderItemDelta.Full)
                Full = to.Full;

            return d;
        }

        public static RenderItemDelta Compare(RenderItemVersion v, RenderItemVersion vref)
        {
            RenderItemDelta ret = RenderItemDelta.None;

            if (v.Dynamic != vref.Dynamic)
                ret |= RenderItemDelta.Dynamic;

            if(v.Full != vref.Full)
                ret |= RenderItemDelta.Full;

            return ret;
        }
    }
}
