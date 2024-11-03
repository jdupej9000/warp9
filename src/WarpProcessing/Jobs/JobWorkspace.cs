using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Jobs
{
    public class JobWorkspace
    {
        public void Set<T>(string key, int index, T value)
        {
            
        }

        public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
        {
            throw new NotImplementedException();
        }
    }
}
