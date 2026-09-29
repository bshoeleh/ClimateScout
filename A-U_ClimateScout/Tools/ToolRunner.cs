using System.CommandLine;

namespace A_U_ClimateScout.Tools
{
    // Builds the "tool" command tree and runs the command named in args (plan §3.3).
    // Returns the process exit code: 0 = success, anything else = failure.
    public static class ToolRunner
    {
        public static async Task<int> RunAsync(IServiceProvider services, string[] args)
        {
            var root = new RootCommand("ClimateScout data commands (plan §3.3).");
            root.Subcommands.Add(DbCommands.Create(services));
            return await root.Parse(args).InvokeAsync();
        }
    }
}
