using System.Text.Json;
using System.Text.Json.Nodes;
using NpmLink.Cli.Services;

namespace NpmLink.Cli.Tests;

public class NpmLinkServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _workspacePath;
    private readonly string _librarySourcePath;
    private const string LibraryName = "@my-org/my-lib";

    public NpmLinkServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "NpmLinkTests_" + Guid.NewGuid().ToString("N"));
        _workspacePath = Path.Combine(_tempRoot, "workspace");
        _librarySourcePath = Path.Combine(_tempRoot, "my-lib");

        Directory.CreateDirectory(_workspacePath);
        Directory.CreateDirectory(_librarySourcePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private void CreateAngularJson() =>
        File.WriteAllText(Path.Combine(_workspacePath, "angular.json"), "{}");

    private void CreateLibraryPackageJson(string name = LibraryName) =>
        File.WriteAllText(
            Path.Combine(_librarySourcePath, "package.json"),
            JsonSerializer.Serialize(new { name }));

    // ── Validation tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task LinkAsync_MissingWorkspacePath_ReturnsOne()
    {
        var runner = new FakeProcessRunner();
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(
            Path.Combine(_tempRoot, "nonexistent"),
            LibraryName,
            _librarySourcePath);

        Assert.Equal(1, result);
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task LinkAsync_MissingLibrarySourcePath_ReturnsOne()
    {
        var runner = new FakeProcessRunner();
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(
            _workspacePath,
            LibraryName,
            Path.Combine(_tempRoot, "nonexistent"));

        Assert.Equal(1, result);
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task LinkAsync_MissingAngularJson_ReturnsOne()
    {
        CreateLibraryPackageJson();
        // No angular.json created in workspace
        var runner = new FakeProcessRunner();
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result);
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task LinkAsync_PackageJsonNameMismatch_ReturnsOne()
    {
        CreateAngularJson();
        CreateLibraryPackageJson("@other-org/other-lib"); // wrong name
        var runner = new FakeProcessRunner();
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result);
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task LinkAsync_MissingPackageJson_ReturnsOne()
    {
        CreateAngularJson();
        // No package.json in library source
        var runner = new FakeProcessRunner();
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result);
        Assert.Empty(runner.Invocations);
    }

    // ── Process invocation tests ──────────────────────────────────────────────

    [Fact]
    public async Task LinkAsync_ValidInputs_RunsNpmLinkInLibrarySourceFirst()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var runner = new FakeProcessRunner(0, 0);
        var service = new NpmLinkService(runner);

        await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(2, runner.Invocations.Count);
        var (_, firstArgs, firstDir) = runner.Invocations[0];
        Assert.Contains("link", firstArgs);
        Assert.Equal(Path.GetFullPath(_librarySourcePath), firstDir);
    }

    [Fact]
    public async Task LinkAsync_ValidInputs_RunsNpmLinkWithLibraryNameInWorkspace()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var runner = new FakeProcessRunner(0, 0);
        var service = new NpmLinkService(runner);

        await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(2, runner.Invocations.Count);
        var (_, secondArgs, secondDir) = runner.Invocations[1];
        Assert.Contains(LibraryName, secondArgs);
        Assert.Equal(Path.GetFullPath(_workspacePath), secondDir);
    }

    [Fact]
    public async Task LinkAsync_ValidInputs_ReturnsZeroOnSuccess()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var runner = new FakeProcessRunner(0, 0);
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task LinkAsync_NpmLinkInLibraryFails_ReturnsNonZeroAndDoesNotLinkInWorkspace()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var runner = new FakeProcessRunner(1); // first npm link fails
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.NotEqual(0, result);
        Assert.Single(runner.Invocations); // second call should not happen
    }

    [Fact]
    public async Task LinkAsync_NpmLinkInWorkspaceFails_ReturnsNonZero()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var runner = new FakeProcessRunner(0, 1); // second npm link fails
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.NotEqual(0, result);
    }

    // ── tsconfig.json path update tests ──────────────────────────────────────

    [Fact]
    public async Task LinkAsync_WithTsconfig_UpdatesPathMappings()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, """{"compilerOptions": {}}""");

        var runner = new FakeProcessRunner(0, 0);
        var service = new NpmLinkService(runner);

        await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        var updatedContent = File.ReadAllText(tsconfigPath);
        var tsconfig = JsonNode.Parse(updatedContent);
        var paths = tsconfig?["compilerOptions"]?["paths"]?.AsObject();

        Assert.NotNull(paths);
        Assert.True(paths.ContainsKey(LibraryName), $"Expected '{LibraryName}' key in paths");
        Assert.True(paths.ContainsKey($"{LibraryName}/*"), $"Expected '{LibraryName}/*' key in paths");
    }

    [Fact]
    public async Task LinkAsync_WithoutTsconfig_SucceedsWithoutError()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        // No tsconfig.json - should still succeed

        var runner = new FakeProcessRunner(0, 0);
        var service = new NpmLinkService(runner);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task LinkAsync_TsconfigPathValues_ContainRelativePath()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, """{"compilerOptions": {}}""");

        var runner = new FakeProcessRunner(0, 0);
        var service = new NpmLinkService(runner);

        await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        var updatedContent = File.ReadAllText(tsconfigPath);
        var tsconfig = JsonNode.Parse(updatedContent);
        var paths = tsconfig?["compilerOptions"]?["paths"]?.AsObject();

        var exactEntries = paths?[LibraryName]?.AsArray();
        Assert.NotNull(exactEntries);
        Assert.Single(exactEntries);

        var expectedRelative = Path.GetRelativePath(_workspacePath, _librarySourcePath).Replace('\\', '/');
        Assert.Equal(expectedRelative, exactEntries![0]!.GetValue<string>());
    }
}
