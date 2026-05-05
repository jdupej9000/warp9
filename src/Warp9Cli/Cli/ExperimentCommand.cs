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
            ExperimentKinds = ek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public string[] ExperimentKinds { get; init; }

        public void Execute(CommandExecutionContext ctx)
        {
            foreach (string k in ExperimentKinds)
            {
                if (k == "trigrid-nn")
                    Experiments.TrigridNnSearch(2048, 16);
                else if (k == "trigrid-raycast")
                    Experiments.TrigridRaycast(2048, 16);
            }
        }
    }
}
