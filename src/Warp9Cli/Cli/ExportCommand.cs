using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9Cli.Cli
{
    public enum ExportFormat
    { 
        Default = 0,
        Csv = 1
    }


    public class ExportCommandSpec : ICommandSpec
    {
        public string? Option => "e";

        public string? LongOption => "export";

        public ICommand Parse(CommandTokens tokens)
        {
            throw new NotImplementedException();
        }
    }

    public class ExportCommand : ICommand
    {
        public ExportCommand(ExportFormat fmt, long item, string? subitem)
        {

        }

        public void Execute(CommandExecutionContext ctx)
        {
            
        }
    }
}
