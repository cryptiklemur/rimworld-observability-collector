using System;

namespace Cryptiklemur.RimObs.Collector.Cli;

public static class CliRouter
{
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintHelp();
            return 0;
        }

        if (args[0] == "version" || args[0] == "--version")
        {
            Console.WriteLine($"{Wire.BuildInfo.Revision} (built {Wire.BuildInfo.BuildTime})");
            return 0;
        }

        Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Collector — RimWorld Observability Collector");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Collector serve         Start the daemon and dashboard.");
        Console.WriteLine("  Collector version       Print version and exit.");
        Console.WriteLine("  Collector --help        Show this help.");
    }
}
