using System;
using System.Collections.Generic;
using System.Text;
using Warp9.Native;

namespace Warp9Cli.Cli
{
    public class InfoCommandSpec : ICommandSpec
    {
        public string? Option => null;
        public string? LongOption => "info";

        public ICommand Parse(CommandTokens tokens)
        {
            return new InfoCommand();
        }
    }

    public class InfoCommand : ICommand
    {
        public void Execute(CommandExecutionContext ctx)
        {
            const int MaxDataLen = 1024;
            StringBuilder sb = new StringBuilder(MaxDataLen);

            foreach (WarpCoreInfoIndex idx in Enum.GetValues(typeof(WarpCoreInfoIndex)))
            {
                int len = WarpCore.wcore_get_info((int)idx, sb, MaxDataLen);
                Console.WriteLine(idx.ToString() + ": " + sb.ToString());
            }
        }
    }
}
