using FileConventionValidator.Models;
using FileConventionValidator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileConventionValidator
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("File Convention Validator");
                Console.WriteLine("========================");

                // Build configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddCommandLine(args)
                    .Build();

                // Build host with dependency injection
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((context, services) =>
                    {
                        // Configure settings
                        services.Configure<FileConventionSettings>(configuration.GetSection("FileConventionSettings"));

                        // Register services
                        services.AddTransient<IFileConventionValidationService, FileConventionValidationService>();
                        services.AddTransient<IFileProcessingService, FileProcessingService>();
                        services.AddTransient<FileConventionValidatorApp>();
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.AddDebug();
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                    .UseConsoleLifetime()
                    .Build();

                // Run the application
                var app = host.Services.GetRequiredService<FileConventionValidatorApp>();
                var exitCode = await app.RunAsync(args);

                Console.WriteLine($"Application completed with exit code: {exitCode}");
                return exitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }
    }

    /// <summary>
    /// Main application class that orchestrates file validation processing
    /// </summary>
    public class FileConventionValidatorApp
    {
        private readonly IFileProcessingService _fileProcessingService;
        private readonly ILogger<FileConventionValidatorApp> _logger;
        private readonly IConfiguration _configuration;

        public FileConventionValidatorApp(
            IFileProcessingService fileProcessingService,
            ILogger<FileConventionValidatorApp> logger,
            IConfiguration configuration)
        {
            _fileProcessingService = fileProcessingService ?? throw new ArgumentNullException(nameof(fileProcessingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<int> RunAsync(string[] args)
        {
            try
            {
                _logger.LogInformation("File Convention Validator starting");

                // Load settings
                var settings = new FileConventionSettings();
                _configuration.GetSection("FileConventionSettings").Bind(settings);

                // Validate settings
                if (!ValidateSettings(settings))
                {
                    return 1;
                }

                // Display configuration
                DisplayConfiguration(settings);

                // Process command line arguments for overrides
                ProcessCommandLineArguments(args, settings);

                // Run file processing
                var result = await _fileProcessingService.ProcessDirectoryAsync(settings);

                // Display results
                DisplayResults(result);

                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application error");
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private bool ValidateSettings(FileConventionSettings settings)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(settings.InputDirectory))
                errors.Add("InputDirectory is required in configuration");

            if (string.IsNullOrWhiteSpace(settings.ReadyDirectory))
                errors.Add("ReadyDirectory is required in configuration");

            if (string.IsNullOrWhiteSpace(settings.ReviewDirectory))
                errors.Add("ReviewDirectory is required in configuration");

            if (!settings.NamingConventions.Any())
                errors.Add("At least one NamingConvention must be configured");

            if (errors.Any())
            {
                Console.WriteLine("Configuration errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                return false;
            }

            return true;
        }

        private void DisplayConfiguration(FileConventionSettings settings)
        {
            Console.WriteLine("\nConfiguration:");
            Console.WriteLine($"  Input Directory: {settings.InputDirectory}");
            Console.WriteLine($"  Ready Directory: {settings.ReadyDirectory}");
            Console.WriteLine($"  Review Directory: {settings.ReviewDirectory}");
            Console.WriteLine($"  Create Directories: {settings.CreateDirectories}");
            Console.WriteLine($"  Process Subdirectories: {settings.ProcessSubdirectories}");

            if (settings.FileExtensions.Any())
            {
                Console.WriteLine($"  File Extensions: {string.Join(", ", settings.FileExtensions)}");
            }
            else
            {
                Console.WriteLine("  File Extensions: All files");
            }

            Console.WriteLine($"  Naming Conventions ({settings.NamingConventions.Count}):");
            foreach (var convention in settings.NamingConventions)
            {
                Console.WriteLine($"    - {convention.Name}: {convention.Pattern}");
                if (!string.IsNullOrEmpty(convention.Description))
                {
                    Console.WriteLine($"      Description: {convention.Description}");
                }
            }

            Console.WriteLine($"  Validation Settings:");
            Console.WriteLine($"    - Case Sensitive: {settings.ValidationSettings.CaseSensitive}");
            Console.WriteLine($"    - Allow Spaces: {settings.ValidationSettings.AllowSpaces}");
            Console.WriteLine($"    - Max Length: {settings.ValidationSettings.MaxFileNameLength}");
            Console.WriteLine($"    - Min Length: {settings.ValidationSettings.MinFileNameLength}");
        }

        private void ProcessCommandLineArguments(string[] args, FileConventionSettings settings)
        {
            // Simple command line argument processing
            // Format: FileConventionValidator [InputPath] [ReadyPath] [ReviewPath]
            if (args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0]))
            {
                settings.InputDirectory = args[0];
                Console.WriteLine($"Input directory overridden from command line: {settings.InputDirectory}");
            }

            if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
            {
                settings.ReadyDirectory = args[1];
                Console.WriteLine($"Ready directory overridden from command line: {settings.ReadyDirectory}");
            }

            if (args.Length >= 3 && !string.IsNullOrWhiteSpace(args[2]))
            {
                settings.ReviewDirectory = args[2];
                Console.WriteLine($"Review directory overridden from command line: {settings.ReviewDirectory}");
            }
        }

        private void DisplayResults(ValidationResult result)
        {
            Console.WriteLine("\nProcessing Results:");
            Console.WriteLine("==================");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Total Files Processed: {result.TotalFilesProcessed}");
            Console.WriteLine($"Files Moved to Ready: {result.FilesMovedToReady}");
            Console.WriteLine($"Files Moved to Review: {result.FilesMovedToReview}");
            Console.WriteLine($"Errors Encountered: {result.ErrorsEncountered}");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error Message: {result.ErrorMessage}");
            }

            if (result.ProcessedFiles.Any())
            {
                Console.WriteLine("\nFile Details:");
                Console.WriteLine("=============");

                foreach (var file in result.ProcessedFiles)
                {
                    Console.WriteLine($"\nFile: {file.FileName}");
                    Console.WriteLine($"  Valid: {file.IsValid}");
                    Console.WriteLine($"  Action: {file.Action}");

                    if (!string.IsNullOrEmpty(file.MatchedConvention))
                    {
                        Console.WriteLine($"  Matched Convention: {file.MatchedConvention}");
                    }

                    if (!string.IsNullOrEmpty(file.DestinationPath))
                    {
                        Console.WriteLine($"  Moved to: {file.DestinationPath}");
                    }

                    if (file.ValidationErrors.Any())
                    {
                        Console.WriteLine($"  Validation Errors:");
                        foreach (var error in file.ValidationErrors)
                        {
                            Console.WriteLine($"    - {error}");
                        }
                    }
                }
            }

            // Summary message
            Console.WriteLine($"\nSummary: {result.FilesMovedToReady} files ready, {result.FilesMovedToReview} files need review");

            if (result.ErrorsEncountered > 0)
            {
                Console.WriteLine($"Warning: {result.ErrorsEncountered} files had processing errors");
            }
        }
    }
}
