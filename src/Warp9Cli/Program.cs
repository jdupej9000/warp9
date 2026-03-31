using Warp9Cli.Cli;

namespace Warp9Cli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CliParser parser = CreateParser();
            List<ICommand> commands = new List<ICommand>();
            try
            {
                parser.Parse(commands, args);
            }
            catch (CliParserException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            using CommandExecutionContext context = new CommandExecutionContext();
            foreach(ICommand cmd in commands)
                cmd.Execute(context);

        }

        static CliParser CreateParser()
        {
            CliParser parser = new CliParser();
            parser.AddSpec(new InfoCommandSpec());
            parser.AddSpec(new LoadProjectCommandSpec());
            parser.AddSpec(new ProjectListingCommandSpec());
            return parser;
        }
    }
}
