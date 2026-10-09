using Lattice.Core;
namespace Lattice.Cli;
internal static class Program
{
    private static int Main(string[] args)
    {
        var debug = Array.Exists(args, arg => arg is "--debug" or "-d");
        var registry = new ToolRegistry(new ITool[] { new CalculatorTool() });
        var loop = new AgentLoop(registry, ToolPermissionPolicy.ReadOnlyOnly);
        var session = Session.Empty(SessionId.New());
        Console.WriteLine("Lattice demo. Controlled statements: fact | goal | constraint | action. Type 'exit' to quit.");
        if (debug)
        {
            Console.WriteLine("Debug trace enabled.");
        }
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
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
        var parsed = ControlledLanguageParser.Parse(line);
        if (!parsed.IsSuccess)
        {
            Console.WriteLine($"parse error [{parsed.Error!.Code}]: {parsed.Error.Message}");
            return session;
        }
        if (debug)
        {
            Console.WriteLine($"  trace: {TurnTrace.DescribeStatement(parsed.Value)}");
        }
        var turn = loop.Run(session, parsed.Value);
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