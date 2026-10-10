using Lattice.Core;

namespace Lattice.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        bool debug = Array.Exists(args, arg => arg is "--debug" or "-d");
        ToolRegistry registry = new([new CalculatorTool()]);
        AgentLoop loop = new(registry, ToolPermissionPolicy.ReadOnlyOnly);
        Session session = Session.Empty(SessionId.New());
        Console.WriteLine("Lattice demo. Controlled statements: fact | goal | constraint | action. Type 'exit' to quit.");
        if (debug)
        {
            Console.WriteLine("Debug trace enabled.");
        }

        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line is null)
            {
                break;
            }

            if (line.Trim() is "exit" or "quit")
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            session = RunTurn(loop, session, line, debug);
        }

        return 0;
    }

    private static Session RunTurn(AgentLoop loop, Session session, string line, bool debug)
    {
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse(line);
        if (!parsed.IsSuccess)
        {
            Console.WriteLine($"parse error [{parsed.Error!.Code}]: {parsed.Error.Message}");
            return session;
        }

        if (debug)
        {
            Console.WriteLine($"  trace: {TurnTrace.DescribeStatement(parsed.Value)}");
        }

        AgentTurnResult turn = loop.Run(session, parsed.Value);
        if (debug)
        {
            Console.WriteLine($"  trace: outcome={turn.Outcome}");
            if (turn.Invocation is not null)
            {
                Console.WriteLine($"  trace: {TurnTrace.DescribeInvocation(turn.Invocation)}");
            }
        }

        Console.WriteLine(turn.Response);
        return turn.Session;
    }
}