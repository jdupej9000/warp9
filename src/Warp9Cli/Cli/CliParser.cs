using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SharpDX.Direct3D11;

namespace Warp9Cli.Cli
{
    public class CliParserException : Exception
    {
        public CliParserException(int tokIdx, string msg) :
           base(msg)
        {
            TokenIndex = tokIdx;
        }

        public CliParserException(int tokIdx, string[] args, string msg) :
            base(msg)
        {
            TokenIndex = tokIdx;
        }

        public int TokenIndex { get; init; }

        public static void ThrowInvalidCommand(int tokenIndex, string[] args)
        {
            throw new CliParserException(tokenIndex, args, "Invalid command.");
        }

        public static void ThrowUnknownCommand(int tokenIndex, string[] args)
        {
            throw new CliParserException(tokenIndex, args, "Command not recognized.");
        }
    }



    // Allowed command formats:
    // -c               (command c)
    // -c:s             (command c -> subcommand s)
    // -c p0            (command c, params: p0)
    // -c p0 p1         (command c, params: p0 p1)
    // -c p0 p4=v4      (command c, params: p0 (null) (null) (null) p4=v4)
    // --command
    public class CliParser
    {
        Dictionary<string, ICommandSpec> specs = new Dictionary<string, ICommandSpec>();

        public void AddSpec(ICommandSpec spec)
        {
            if (spec.Option is not null)
                specs.Add(spec.Option, spec);

            if (spec.LongOption is not null)
                specs.Add(spec.LongOption, spec);

        }

        public void Parse(List<ICommand> commands, string[] args)
        {
            int pos = 0;
            while (pos < args.Length)
            {
                int numTaken = TokenizeNextCommand(pos, args, out CommandTokens tokens);
                if (numTaken == 0)
                    break;

                if (!specs.TryGetValue(tokens.Command, out ICommandSpec? spec) ||
                    spec is null)
                    CliParserException.ThrowUnknownCommand(numTaken, args);

                ICommand cmd = spec.Parse(tokens);
               
                commands.Add(cmd);
                pos += numTaken;
            }

        }

        public int TokenizeNextCommand(int start, string[] args, out CommandTokens tokens)
        {
            int pos = start;
            tokens = null;

            if (pos >= args.Length)
                return pos - start;

                    
            CommandTokens? tokensParsed = ParseCommand(args[pos++]);
            if (tokensParsed is null)
                CliParserException.ThrowInvalidCommand(pos - 1, args);

            tokens = tokensParsed;
            tokens.FirstTokenIndex = start;

            int parIndex = 0;
            while (pos < args.Length)
            {
                CommandParameter? par = ParseParameter(args[pos]);
                if (par is null)
                    break;

                par.Index = parIndex;
                tokens.Params.Add(par);

                parIndex++;
                pos++;
            }

            return pos - start;
        }

        private CommandTokens? ParseCommand(string arg)
        {
            if (arg.StartsWith("-"))
            {
                int keyStart = arg.StartsWith("--") ? 2 : 1;
                int subcStart = arg.IndexOf(':');

                if (subcStart > keyStart)
                {
                    return new CommandTokens()
                    {
                        IsCommandLong = keyStart > 1,
                        Command = arg.Substring(keyStart, subcStart - keyStart),
                        Subcommand = arg.Substring(subcStart + 1)
                    };
                }

                return new CommandTokens()
                {
                    IsCommandLong = keyStart > 1,
                    Command = arg.Substring(keyStart)
                };
            }
                
            return null;
        }

        private CommandParameter? ParseParameter(string arg)
        {
            if (arg.StartsWith('-'))
                return null;

            if (arg.StartsWith('\"') && arg.EndsWith('\"'))
            {
                return new CommandParameter()
                {
                    RawValue = arg.Substring(1, arg.Length - 2)
                };
            }

            int sepIndex = arg.IndexOf('=');
            if (sepIndex > 0)
            {
                return new CommandParameter()
                {
                    Name = arg.Substring(0, sepIndex),
                    RawValue = arg.Substring(sepIndex + 1)
                };
            }

            return new CommandParameter()
            {
                RawValue = arg
            };
        }

    }
}
