using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Utils
{
    public interface IUntypedTableProvider
    {
        public IEnumerable<string[]> ParsedData { get; }
        public string WorkingDirectory { get; }
    }
}
