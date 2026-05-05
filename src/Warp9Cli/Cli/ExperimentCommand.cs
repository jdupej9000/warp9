using System;
using System.Collections.Generic;
using System.Text;
using Warp9.Native;

namespace Warp9Cli.Cli
{
    public class ExperimentCommandSpec : ICommandSpec
    {
        public string? Option => null;
        public string? LongOption => "experiment";

        public ICommand Parse(CommandTokens tokens)
        {
            if (tokens.Params.Count != 1)
                throw new CliParserException(tokens.FirstTokenIndex, "Experiment kind must be specified.");

            return new ExperimentCommandCommand(tokens.Params[0].RawValue);
        }
    }

    public class ExperimentCommandCommand : ICommand
    {
        public ExperimentCommandCommand(string ek)
        {
            ExperimentKind = ek;
        }

        public string ExperimentKind { get; init; }

        public void Execute(CommandExecutionContext ctx)
        {
            if (ExperimentKind == "trigrid-nn")
                Experiments.TrigridNnSearch(2048, 16);
        }
    }
}
