using NpmLink.Cli.Services;

namespace NpmLink.Cli.Commands;

internal static class CommandResultRenderer
{
    public static void Render(OperationResult result)
    {
        foreach (var message in result.Messages)
        {
            if (message.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("FAIL:", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(message);
                continue;
            }

            Console.WriteLine(message);
        }
    }
}
