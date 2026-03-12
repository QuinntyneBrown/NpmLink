@echo off
setlocal

set PROJECT_DIR=%~dp0..\..\src\NpmLink.Cli

echo Packing NpmLink.Cli...
dotnet pack "%PROJECT_DIR%" -c Release -o "%PROJECT_DIR%\nupkg"
if %errorlevel% neq 0 (
    echo Error: dotnet pack failed.
    exit /b %errorlevel%
)

echo Installing npm-link tool globally...
dotnet tool install --global --add-source "%PROJECT_DIR%\nupkg" NpmLink.Cli
if %errorlevel% neq 0 (
    echo Tool already installed. Updating...
    dotnet tool update --global --add-source "%PROJECT_DIR%\nupkg" NpmLink.Cli
    if %errorlevel% neq 0 (
        echo Error: dotnet tool update failed.
        exit /b %errorlevel%
    )
)

echo.
echo npm-link tool installed successfully. Run 'npm-link --help' to get started.
