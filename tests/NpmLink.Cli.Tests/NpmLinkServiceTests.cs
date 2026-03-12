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

    private NpmLinkService CreateService(FakeNpmClient npmClient, ITsConfigEditor? tsConfigEditor = null)
    {
        return new NpmLinkService(npmClient, tsConfigEditor ?? new TsConfigEditor());
    }

    // ── Validation tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task LinkAsync_MissingWorkspacePath_ReturnsOne()
    {
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(
            Path.Combine(_tempRoot, "nonexistent"),
            LibraryName,
            _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(npmClient.Invocations);
    }

    [Fact]
    public async Task LinkAsync_MissingLibrarySourcePath_ReturnsOne()
    {
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(
            _workspacePath,
            LibraryName,
            Path.Combine(_tempRoot, "nonexistent"));

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(npmClient.Invocations);
    }

    [Fact]
    public async Task LinkAsync_MissingAngularJson_ReturnsOne()
    {
        CreateLibraryPackageJson();
        // No angular.json created in workspace
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(npmClient.Invocations);
    }

    [Fact]
    public async Task LinkAsync_PackageJsonNameMismatch_ReturnsOne()
    {
        CreateAngularJson();
        CreateLibraryPackageJson("@other-org/other-lib"); // wrong name
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(npmClient.Invocations);
    }

    [Fact]
    public async Task LinkAsync_MissingPackageJson_ReturnsOne()
    {
        CreateAngularJson();
        // No package.json in library source
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(npmClient.Invocations);
    }

    // ── Process invocation tests ──────────────────────────────────────────────

    [Fact]
    public async Task LinkAsync_ValidInputs_RunsNpmLinkInLibrarySourceFirst()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(2, npmClient.Invocations.Count);
        var (method, _, dir) = npmClient.Invocations[0];
        Assert.Equal("LinkGlobal", method);
        Assert.Equal(Path.GetFullPath(_librarySourcePath), dir);
    }

    [Fact]
    public async Task LinkAsync_ValidInputs_RunsNpmLinkWithLibraryNameInWorkspace()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(2, npmClient.Invocations.Count);
        var (method, libName, dir) = npmClient.Invocations[1];
        Assert.Equal("LinkIntoWorkspace", method);
        Assert.Equal(LibraryName, libName);
        Assert.Equal(Path.GetFullPath(_workspacePath), dir);
    }

    [Fact]
    public async Task LinkAsync_ValidInputs_ReturnsZeroOnSuccess()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task LinkAsync_NpmLinkInLibraryFails_ReturnsNonZeroAndDoesNotLinkInWorkspace()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var npmClient = new FakeNpmClient(1); // first npm link fails
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Single(npmClient.Invocations); // second call should not happen
    }

    [Fact]
    public async Task LinkAsync_NpmLinkInWorkspaceFails_ReturnsNonZero()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();
        var npmClient = new FakeNpmClient(0, 1); // second npm link fails
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.NotEqual(0, result.ExitCode);
    }

    // ── tsconfig.json path update tests ──────────────────────────────────────

    [Fact]
    public async Task LinkAsync_WithTsconfig_UpdatesPathMappings()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, """{"compilerOptions": {}}""");

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

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

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task LinkAsync_TsconfigPathValues_ContainRelativePath()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, """{"compilerOptions": {}}""");

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

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

    // ── Unlink: Validation tests ─────────────────────────────────────────────

    [Fact]
    public async Task UnlinkAsync_MissingWorkspacePath_ReturnsOne()
    {
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(
            Path.Combine(_tempRoot, "nonexistent"),
            LibraryName,
            _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(npmClient.Invocations);
    }

    [Fact]
    public async Task UnlinkAsync_MissingAngularJson_ReturnsOne()
    {
        // No angular.json created in workspace
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(npmClient.Invocations);
    }

    // ── Unlink: Process invocation tests ─────────────────────────────────────

    [Fact]
    public async Task UnlinkAsync_ValidInputs_RunsNpmUnlinkInWorkspaceFirst()
    {
        CreateAngularJson();
        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(2, npmClient.Invocations.Count);
        var (method, libName, dir) = npmClient.Invocations[0];
        Assert.Equal("UnlinkFromWorkspace", method);
        Assert.Equal(LibraryName, libName);
        Assert.Equal(Path.GetFullPath(_workspacePath), dir);
    }

    [Fact]
    public async Task UnlinkAsync_ValidInputs_RunsNpmUnlinkInLibrarySourceSecond()
    {
        CreateAngularJson();
        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(2, npmClient.Invocations.Count);
        var (method, _, dir) = npmClient.Invocations[1];
        Assert.Equal("UnlinkGlobal", method);
        Assert.Equal(Path.GetFullPath(_librarySourcePath), dir);
    }

    [Fact]
    public async Task UnlinkAsync_ValidInputs_ReturnsZeroOnSuccess()
    {
        CreateAngularJson();
        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task UnlinkAsync_FirstNpmUnlinkFails_StopsAndPropagatesExitCode()
    {
        CreateAngularJson();
        var npmClient = new FakeNpmClient(2); // first npm unlink fails with exit code 2
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(2, result.ExitCode);
        Assert.Single(npmClient.Invocations); // second call should not happen
    }

    [Fact]
    public async Task UnlinkAsync_SecondNpmUnlinkFails_PropagatesExitCode()
    {
        CreateAngularJson();
        var npmClient = new FakeNpmClient(0, 3); // second npm unlink fails with exit code 3
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(3, result.ExitCode);
    }

    // ── Unlink: tsconfig.json path removal tests ─────────────────────────────

    [Fact]
    public async Task UnlinkAsync_WithTsconfig_RemovesLibraryPaths()
    {
        CreateAngularJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        var tsconfigContent = JsonSerializer.Serialize(new
        {
            compilerOptions = new
            {
                paths = new Dictionary<string, string[]>
                {
                    [LibraryName] = new[] { "../my-lib" },
                    [$"{LibraryName}/*"] = new[] { "../my-lib/*" },
                    ["@other/lib"] = new[] { "../other-lib" },
                }
            }
        });
        File.WriteAllText(tsconfigPath, tsconfigContent);

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        var updatedContent = File.ReadAllText(tsconfigPath);
        var tsconfig = JsonNode.Parse(updatedContent);
        var paths = tsconfig?["compilerOptions"]?["paths"]?.AsObject();

        Assert.NotNull(paths);
        Assert.False(paths.ContainsKey(LibraryName), $"Expected '{LibraryName}' key to be removed from paths");
        Assert.False(paths.ContainsKey($"{LibraryName}/*"), $"Expected '{LibraryName}/*' key to be removed from paths");
    }

    [Fact]
    public async Task UnlinkAsync_WithTsconfig_PreservesOtherPaths()
    {
        CreateAngularJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        var tsconfigContent = JsonSerializer.Serialize(new
        {
            compilerOptions = new
            {
                paths = new Dictionary<string, string[]>
                {
                    [LibraryName] = new[] { "../my-lib" },
                    [$"{LibraryName}/*"] = new[] { "../my-lib/*" },
                    ["@other/lib"] = new[] { "../other-lib" },
                    ["@other/lib/*"] = new[] { "../other-lib/*" },
                }
            }
        });
        File.WriteAllText(tsconfigPath, tsconfigContent);

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        var updatedContent = File.ReadAllText(tsconfigPath);
        var tsconfig = JsonNode.Parse(updatedContent);
        var paths = tsconfig?["compilerOptions"]?["paths"]?.AsObject();

        Assert.NotNull(paths);
        Assert.True(paths.ContainsKey("@other/lib"), "Expected '@other/lib' key to be preserved");
        Assert.True(paths.ContainsKey("@other/lib/*"), "Expected '@other/lib/*' key to be preserved");
    }

    [Fact]
    public async Task UnlinkAsync_WithoutTsconfig_SucceedsWithoutError()
    {
        CreateAngularJson();
        // No tsconfig.json - should still succeed

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task UnlinkAsync_TsconfigWithoutPathKeys_SucceedsWithoutError()
    {
        CreateAngularJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, """{"compilerOptions": {"paths": {}}}""");

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    // ── Verify: Validation tests ────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_MissingWorkspacePath_ReturnsOne()
    {
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(
            Path.Combine(_tempRoot, "nonexistent"),
            LibraryName,
            _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_MissingAngularJson_ReturnsOne()
    {
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    // ── Verify: Symlink tests ───────────────────────────────────────────────

    private static bool TryCreateSymbolicLink(string linkPath, string targetPath)
    {
        try
        {
            // Ensure parent directory exists
            var parent = Path.GetDirectoryName(linkPath);
            if (parent is not null)
                Directory.CreateDirectory(parent);

            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    [Fact]
    public async Task VerifyAsync_SymlinkExists_ReportsPass()
    {
        CreateAngularJson();
        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);

        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            // Skip: symlink creation not supported in this environment
            return;
        }

        // Also set up tsconfig so it doesn't fail on that check
        CreateTsconfigWithPaths();

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_SymlinkMissing_ReportsFail()
    {
        CreateAngularJson();
        CreateTsconfigWithPaths();
        // No symlink created

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_NotASymlink_ReportsFail()
    {
        CreateAngularJson();
        CreateTsconfigWithPaths();

        // Create a regular directory instead of a symlink
        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        Directory.CreateDirectory(nodeModulesLibPath);

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_SymlinkWrongTarget_ReportsFail()
    {
        CreateAngularJson();
        CreateTsconfigWithPaths();

        var wrongTarget = Path.Combine(_tempRoot, "wrong-target");
        Directory.CreateDirectory(wrongTarget);

        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);

        if (!TryCreateSymbolicLink(nodeModulesLibPath, wrongTarget))
        {
            // Skip: symlink creation not supported in this environment
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    // ── Verify: tsconfig tests ──────────────────────────────────────────────

    private void CreateTsconfigWithPaths()
    {
        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        var relPath = Path.GetRelativePath(_workspacePath, _librarySourcePath).Replace('\\', '/');
        var content = JsonSerializer.Serialize(new
        {
            compilerOptions = new
            {
                paths = new Dictionary<string, string[]>
                {
                    [LibraryName] = new[] { relPath },
                    [$"{LibraryName}/*"] = new[] { $"{relPath}/*" },
                }
            }
        });
        File.WriteAllText(tsconfigPath, content);
    }

    [Fact]
    public async Task VerifyAsync_TsconfigWithPaths_ReportsPass()
    {
        CreateAngularJson();
        CreateTsconfigWithPaths();

        // Create a symlink so the symlink check also passes
        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            // Skip: symlink creation not supported in this environment
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_TsconfigMissing_ReportsFail()
    {
        CreateAngularJson();
        // No tsconfig.json

        // Create symlink so only tsconfig fails
        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            // Skip: symlink creation not supported in this environment
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_TsconfigMissingPaths_ReportsFail()
    {
        CreateAngularJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, """{"compilerOptions": {}}""");

        // Create symlink so only tsconfig fails
        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_TsconfigPartialPaths_ReportsFail()
    {
        CreateAngularJson();

        // Only the exact key, missing wildcard
        var relPath = Path.GetRelativePath(_workspacePath, _librarySourcePath).Replace('\\', '/');
        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        var content = JsonSerializer.Serialize(new
        {
            compilerOptions = new
            {
                paths = new Dictionary<string, string[]>
                {
                    [LibraryName] = new[] { relPath },
                }
            }
        });
        File.WriteAllText(tsconfigPath, content);

        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    // ── Verify: Aggregate result tests ──────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_AllChecksPass_ReturnsZero()
    {
        CreateAngularJson();
        CreateTsconfigWithPaths();

        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_AnyCheckFails_ReturnsOne()
    {
        CreateAngularJson();
        // Symlink missing + tsconfig missing = both fail

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    // ── Item 7: Verify path value tests ─────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_ExactMappingWrongPath_ReturnsOne()
    {
        CreateAngularJson();

        var relPath = Path.GetRelativePath(_workspacePath, _librarySourcePath).Replace('\\', '/');
        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        var content = JsonSerializer.Serialize(new
        {
            compilerOptions = new
            {
                paths = new Dictionary<string, string[]>
                {
                    [LibraryName] = new[] { "../wrong-path" },
                    [$"{LibraryName}/*"] = new[] { $"{relPath}/*" },
                }
            }
        });
        File.WriteAllText(tsconfigPath, content);

        // Create symlink so only tsconfig value fails
        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_WildcardMappingWrongPath_ReturnsOne()
    {
        CreateAngularJson();

        var relPath = Path.GetRelativePath(_workspacePath, _librarySourcePath).Replace('\\', '/');
        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        var content = JsonSerializer.Serialize(new
        {
            compilerOptions = new
            {
                paths = new Dictionary<string, string[]>
                {
                    [LibraryName] = new[] { relPath },
                    [$"{LibraryName}/*"] = new[] { "../wrong-path/*" },
                }
            }
        });
        File.WriteAllText(tsconfigPath, content);

        // Create symlink so only tsconfig value fails
        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task VerifyAsync_BothMappingsCorrect_ReturnsZero()
    {
        CreateAngularJson();
        CreateTsconfigWithPaths();

        var nodeModulesLibPath = Path.Combine(_workspacePath, "node_modules", LibraryName);
        if (!TryCreateSymbolicLink(nodeModulesLibPath, _librarySourcePath))
        {
            return;
        }

        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.VerifyAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);
    }

    // ── Item 7: JSONC tests ─────────────────────────────────────────────────

    [Fact]
    public async Task LinkAsync_TsconfigWithJsoncComments_Succeeds()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, """
        {
            // This is a comment
            "compilerOptions": {
                "target": "es2020", // inline comment
            }
        }
        """);

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);

        var updatedContent = File.ReadAllText(tsconfigPath);
        var tsconfig = JsonNode.Parse(updatedContent);
        var paths = tsconfig?["compilerOptions"]?["paths"]?.AsObject();
        Assert.NotNull(paths);
        Assert.True(paths.ContainsKey(LibraryName));
    }

    [Fact]
    public async Task UnlinkAsync_TsconfigWithJsoncComments_Succeeds()
    {
        CreateAngularJson();

        var relPath = Path.GetRelativePath(_workspacePath, _librarySourcePath).Replace('\\', '/');
        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, $$"""
        {
            // This is a comment
            "compilerOptions": {
                "paths": {
                    "{{LibraryName}}": ["{{relPath}}"],
                    "{{LibraryName}}/*": ["{{relPath}}/*"],
                }, // trailing comma
            }
        }
        """);

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.Equal(0, result.ExitCode);

        var updatedContent = File.ReadAllText(tsconfigPath);
        var tsconfig = JsonNode.Parse(updatedContent);
        var paths = tsconfig?["compilerOptions"]?["paths"]?.AsObject();
        Assert.NotNull(paths);
        Assert.False(paths.ContainsKey(LibraryName));
    }

    [Fact]
    public async Task LinkAsync_TsconfigInvalidContent_ReturnsNonZero()
    {
        CreateAngularJson();
        CreateLibraryPackageJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, "THIS IS NOT VALID JSON AT ALL {{{");

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task UnlinkAsync_TsconfigInvalidContent_ReturnsNonZero()
    {
        CreateAngularJson();

        var tsconfigPath = Path.Combine(_workspacePath, "tsconfig.json");
        File.WriteAllText(tsconfigPath, "THIS IS NOT VALID JSON AT ALL {{{");

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(_workspacePath, LibraryName, _librarySourcePath);

        Assert.NotEqual(0, result.ExitCode);
    }

    // ── Item 7: Unlink source validation tests ──────────────────────────────

    [Fact]
    public async Task UnlinkAsync_MissingLibrarySourcePath_ReturnsOne()
    {
        CreateAngularJson();
        var npmClient = new FakeNpmClient();
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(
            _workspacePath,
            LibraryName,
            Path.Combine(_tempRoot, "nonexistent"));

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(npmClient.Invocations);
    }

    // ── Item 7: Paths with spaces tests ─────────────────────────────────────

    [Fact]
    public async Task LinkAsync_PathsWithSpaces_Succeeds()
    {
        var workspaceWithSpaces = Path.Combine(_tempRoot, "my workspace");
        var sourceWithSpaces = Path.Combine(_tempRoot, "my library source");
        Directory.CreateDirectory(workspaceWithSpaces);
        Directory.CreateDirectory(sourceWithSpaces);

        File.WriteAllText(Path.Combine(workspaceWithSpaces, "angular.json"), "{}");
        File.WriteAllText(
            Path.Combine(sourceWithSpaces, "package.json"),
            JsonSerializer.Serialize(new { name = LibraryName }));
        File.WriteAllText(Path.Combine(workspaceWithSpaces, "tsconfig.json"), """{"compilerOptions": {}}""");

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.LinkAsync(workspaceWithSpaces, LibraryName, sourceWithSpaces);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2, npmClient.Invocations.Count);

        // Verify tsconfig was updated
        var updatedContent = File.ReadAllText(Path.Combine(workspaceWithSpaces, "tsconfig.json"));
        var tsconfig = JsonNode.Parse(updatedContent);
        var paths = tsconfig?["compilerOptions"]?["paths"]?.AsObject();
        Assert.NotNull(paths);
        Assert.True(paths.ContainsKey(LibraryName));
    }

    [Fact]
    public async Task UnlinkAsync_PathsWithSpaces_Succeeds()
    {
        var workspaceWithSpaces = Path.Combine(_tempRoot, "my workspace");
        var sourceWithSpaces = Path.Combine(_tempRoot, "my library source");
        Directory.CreateDirectory(workspaceWithSpaces);
        Directory.CreateDirectory(sourceWithSpaces);

        File.WriteAllText(Path.Combine(workspaceWithSpaces, "angular.json"), "{}");

        var relPath = Path.GetRelativePath(workspaceWithSpaces, sourceWithSpaces).Replace('\\', '/');
        var tsconfigContent = JsonSerializer.Serialize(new
        {
            compilerOptions = new
            {
                paths = new Dictionary<string, string[]>
                {
                    [LibraryName] = new[] { relPath },
                    [$"{LibraryName}/*"] = new[] { $"{relPath}/*" },
                }
            }
        });
        File.WriteAllText(Path.Combine(workspaceWithSpaces, "tsconfig.json"), tsconfigContent);

        var npmClient = new FakeNpmClient(0, 0);
        var service = CreateService(npmClient);

        var result = await service.UnlinkAsync(workspaceWithSpaces, LibraryName, sourceWithSpaces);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2, npmClient.Invocations.Count);
    }
}
