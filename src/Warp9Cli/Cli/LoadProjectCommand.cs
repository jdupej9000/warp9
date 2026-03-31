using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Warp9.Model;

namespace Warp9Cli.Cli
{
    public class LoadProjectCommandSpec : ICommandSpec
    {
        public string? Option => "o";
        public string? LongOption => "open";

        public ICommand Parse(CommandTokens tokens)
        {
            if (tokens.Params.Count != 1)
                throw new CliParserException(tokens.FirstTokenIndex, "Project path must be specified.");

            return new LoadProjectCommand(tokens.Params[0].RawValue);
        }
    }

    internal class LoadProjectCommand : ICommand
    {
        public LoadProjectCommand(string path)
        {
            Path = path;
        }

        public string Path { get; init; }

        public void Execute(CommandExecutionContext ctx)
        {
            if (ctx.Project is not null || ctx.ProjectArchive is not null)
                throw new InvalidOperationException("A project is already open.");

            DateTime t0 = DateTime.Now;
            ctx.ProjectArchive = new Warp9ProjectArchive(Path, false);
            ctx.Project = Project.Load(ctx.ProjectArchive);
            DateTime t1 = DateTime.Now;

            double timeTaken = (t1 - t0).TotalSeconds;
            Console.WriteLine($"The file '{Path}' has been loaded in {timeTaken:F3} seconds.");
            Console.WriteLine($"  Archive version: {ctx.Project.ProjectVersion}");
        }
    }
}
