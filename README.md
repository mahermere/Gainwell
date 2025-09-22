# C# .NET Development Workspace

This workspace is configured for C# .NET development on Windows with build and debug capabilities.

## What's Included

### Extensions Installed
- **C# for Visual Studio Code** (`ms-dotnettools.csharp`) - IntelliSense, debugging, and project support
- **C# Dev Kit** (`ms-dotnettools.csdevkit`) - Enhanced C# development experience
- **.NET Install Tool** (`ms-dotnettools.vscode-dotnet-runtime`) - .NET runtime management

### Project Structure
- `SampleApp/` - Sample C# console application demonstrating basic functionality
- `.vscode/` - VS Code configuration files
  - `tasks.json` - Build, publish, and watch tasks
  - `launch.json` - Debug configurations
  - `settings.json` - C# development optimized settings

### Build & Debug Features

#### Build Tasks (Ctrl+Shift+P → Tasks: Run Task)
- **build** - Build the project (default build task)
- **publish** - Publish the project for deployment
- **watch** - Run with file watching for development

#### Debug Configurations (F5 to start debugging)
- **.NET Core Launch (console)** - Launch and debug the console application
- **.NET Core Attach** - Attach debugger to running process

#### Quick Commands
```powershell
# Build the project
dotnet build SampleApp/SampleApp.csproj

# Run the project
dotnet run --project SampleApp/SampleApp.csproj

# Watch mode (auto-rebuild on file changes)
dotnet watch run --project SampleApp/SampleApp.csproj
```

### Development Features
- Semantic highlighting enabled
- Format on save enabled
- Roslyn analyzers enabled
- EditorConfig support
- Automatic exclusion of bin/obj folders

## Getting Started
1. Open this workspace in VS Code
2. Press `F5` to build and debug the sample application
3. Set breakpoints in `SampleApp/Program.cs` to test debugging
4. Use `Ctrl+Shift+P` → "Tasks: Run Task" to access build tasks

## .NET Version
This workspace is configured for **.NET 9.0** (current installed version: 9.0.305)
