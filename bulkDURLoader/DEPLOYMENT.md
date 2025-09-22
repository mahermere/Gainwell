# bulkDURLoader Deployment Scripts

## Framework-Dependent Deployment (requires .NET runtime on target machine)

### Build for .NET 6.0
```powershell
dotnet build -c Release -f net6.0
```

### Build for .NET 8.0
```powershell
dotnet build -c Release -f net8.0
```

### Build for .NET 9.0
```powershell
dotnet build -c Release -f net9.0
```

### Publish Framework-Dependent (detects best available runtime)
```powershell
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained false

# Windows x86
dotnet publish -c Release -r win-x86 --self-contained false

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained false
```

## Self-Contained Deployment (includes .NET runtime)

### Publish Self-Contained .NET 6.0
```powershell
# Windows x64 with .NET 6.0
dotnet publish -c Release -f net6.0 -r win-x64 --self-contained true

# Linux x64 with .NET 6.0
dotnet publish -c Release -f net6.0 -r linux-x64 --self-contained true
```

### Publish Self-Contained .NET 8.0
```powershell
# Windows x64 with .NET 8.0
dotnet publish -c Release -f net8.0 -r win-x64 --self-contained true

# Linux x64 with .NET 8.0
dotnet publish -c Release -f net8.0 -r linux-x64 --self-contained true
```

### Publish Self-Contained .NET 9.0
```powershell
# Windows x64 with .NET 9.0
dotnet publish -c Release -f net9.0 -r win-x64 --self-contained true

# Linux x64 with .NET 9.0
dotnet publish -c Release -f net9.0 -r linux-x64 --self-contained true
```

## Single File Executable

### Create Single File for .NET 6.0
```powershell
dotnet publish -c Release -f net6.0 -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

### Create Single File for .NET 8.0
```powershell
dotnet publish -c Release -f net8.0 -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

### Create Single File for .NET 9.0
```powershell
dotnet publish -c Release -f net9.0 -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Deployment Recommendations

1. **Framework-Dependent**: Use when you can guarantee .NET 6.0+ is installed on target machines
2. **Self-Contained**: Use when target machines may not have .NET installed
3. **Single File**: Use for easy distribution and deployment

## Target Machine Requirements

- **Framework-Dependent**: Requires .NET 6.0, 8.0, or 9.0 runtime
- **Self-Contained**: No .NET runtime required
- **Memory**: Minimum 512MB RAM
- **Disk Space**: 50-200MB depending on deployment type