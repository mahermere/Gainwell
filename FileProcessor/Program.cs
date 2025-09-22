using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FileProcessor.Services;

namespace FileProcessor;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Validate we have exactly 2 arguments
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: FileProcessor <input-path> <output-path>");
                Console.Error.WriteLine("  input-path:  Path to the input text file to process");
                Console.Error.WriteLine("  output-path: Path to the output file where valid lines will be written");
                return 1;
            }

            string inputPath = args[0];
            string outputPath = args[1];

            // Build configuration
            var configuration = BuildConfiguration();

            // Build host with dependency injection
            using var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(configuration);
                    services.AddTransient<IFileProcessingService, FileProcessingService>();
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddConfiguration(configuration.GetSection("Logging"));
                    });
                })
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var fileProcessor = host.Services.GetRequiredService<IFileProcessingService>();

            logger.LogInformation("FileProcessor starting...");
            logger.LogInformation("Input file: {InputPath}", inputPath);
            logger.LogInformation("Output file: {OutputPath}", outputPath);

            // Process the file
            var result = await fileProcessor.ProcessFileAsync(inputPath, outputPath);

            if (result.Success)
            {
                logger.LogInformation("File processing completed successfully");
                logger.LogInformation("Total lines processed: {TotalLines}", result.TotalLinesProcessed);
                logger.LogInformation("Valid lines written: {ValidLines}", result.ValidLinesWritten);
                logger.LogInformation("Zero-byte files moved: {ZeroByteFiles}", result.ZeroByteFilesMoved);
                logger.LogInformation("Missing files: {MissingFiles}", result.MissingFiles);
                return 0;
            }
            else
            {
                logger.LogError("File processing failed: {ErrorMessage}", result.ErrorMessage);
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory);

        // Add the default appsettings.json
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
        {
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }

        return builder.Build();
    }
}
