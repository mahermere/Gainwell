using System;
using System.Reflection;
using System.Runtime.InteropServices;
using bulkDURLoader.Logging;

namespace bulkDURLoader.Utilities
{
    public static class RuntimeInfo
    {
        private static readonly Logger _logger = Logger.Instance;

        public static void LogRuntimeInformation()
        {
            try
            {
                var runtimeVersion = Environment.Version;
                var frameworkDescription = RuntimeInformation.FrameworkDescription;
                var osDescription = RuntimeInformation.OSDescription;
                var architecture = RuntimeInformation.OSArchitecture;
                var processArchitecture = RuntimeInformation.ProcessArchitecture;

                _logger.Info("=== Runtime Information ===");
                _logger.Info($"Framework: {frameworkDescription}");
                _logger.Info($"Runtime Version: {runtimeVersion}");
                _logger.Info($"Operating System: {osDescription}");
                _logger.Info($"OS Architecture: {architecture}");
                _logger.Info($"Process Architecture: {processArchitecture}");

                // Determine .NET version
                var dotnetVersion = GetDotNetVersion();
                _logger.Info($".NET Version: {dotnetVersion}");

                Console.WriteLine($"Running on: {frameworkDescription}");
                Console.WriteLine($".NET Version: {dotnetVersion}");
                Console.WriteLine($"OS: {osDescription} ({architecture})");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to retrieve runtime information", ex);
            }
        }

        public static string GetDotNetVersion()
        {
            var version = Environment.Version;

            // Determine major .NET version based on runtime version
            return version.Major switch
            {
                4 => ".NET Framework 4.x",
                6 => ".NET 6.0",
                7 => ".NET 7.0",
                8 => ".NET 8.0",
                9 => ".NET 9.0",
                _ => $".NET {version.Major}.{version.Minor}"
            };
        }

        public static bool IsNet6OrLater()
        {
            return Environment.Version.Major >= 6;
        }

        public static bool IsNet8OrLater()
        {
            return Environment.Version.Major >= 8;
        }

        public static bool IsNet9OrLater()
        {
            return Environment.Version.Major >= 9;
        }

        public static string GetTargetFramework()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var targetFrameworkAttribute = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            return targetFrameworkAttribute?.FrameworkName ?? "Unknown";
        }

        public static void LogAssemblyInformation()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyName = assembly.GetName();
                var location = assembly.Location;
                var targetFramework = GetTargetFramework();

                _logger.Info("=== Assembly Information ===");
                _logger.Info($"Assembly Name: {assemblyName.Name}");
                _logger.Info($"Assembly Version: {assemblyName.Version}");
                _logger.Info($"Assembly Location: {location}");
                _logger.Info($"Target Framework: {targetFramework}");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to retrieve assembly information", ex);
            }
        }
    }
}