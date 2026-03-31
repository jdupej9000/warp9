using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace Warp9Cli.Cli
{
    public class CommandParameter
    {
        public int Index { get; set; } = -1;
        public string? Name { get; init; }
        public required string RawValue { get; init; }
    }

    public class CommandTokens
    {
        public bool IsCommandLong { get; init; }
        public required string Command { get; init; }
        public string? Subcommand { get; init; }
        public int FirstTokenIndex { get; set; }
        public List<CommandParameter> Params { get; init; } = new List<CommandParameter>();
    }

    public interface ICommand
    {
        public void Execute(CommandExecutionContext ctx);  
    }

    public interface ICommandSpec
    {
        public string? Option { get; }
        public string? LongOption { get; }

        public ICommand Parse(CommandTokens tokens);
    }
}
