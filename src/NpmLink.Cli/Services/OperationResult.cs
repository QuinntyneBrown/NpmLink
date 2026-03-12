namespace NpmLink.Cli.Services;

public record OperationResult(int ExitCode, List<string> Messages)
{
    public static OperationResult Success(params string[] messages) =>
        new(0, new List<string>(messages));

    public static OperationResult Failure(params string[] messages) =>
        new(1, new List<string>(messages));

    public static OperationResult Failure(int exitCode, params string[] messages) =>
        new(exitCode, new List<string>(messages));

    public void AddMessage(string message) => Messages.Add(message);
}
