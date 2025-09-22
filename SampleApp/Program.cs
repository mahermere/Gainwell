using System;
using SampleApp.Logging;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize logging system - this will capture all Console.WriteLine calls
            ConsoleLogger.EnableConsoleLogging();
            var logger = Logger.Instance;

            logger.Info("Application started");

            Console.WriteLine("Hello, C# .NET Development!");
            logger.Info("Displayed welcome message");

            // Example with variables for debugging
            string name = "Developer";
            int year = DateTime.Now.Year;

            Console.WriteLine($"Welcome {name}! Current year is {year}");
            logger.Info($"Displayed welcome message for {name} in year {year}");
            logger.Debug($"Variables initialized: name='{name}', year={year}");

            // Example with a loop for setting breakpoints
            logger.Info("Starting iteration loop");
            for (int i = 1; i <= 5; i++)
            {
                Console.WriteLine($"Iteration {i}");
                logger.Debug($"Displayed iteration {i} to console");
                if (i == 3)
                {
                    logger.Warning("Reached iteration 3 - this is a warning example");
                }
            }
            logger.Info("Iteration loop completed");

            // Demonstrate different log levels
            logger.Debug("This is a debug message");
            logger.Info("This is an info message");
            logger.Warning("This is a warning message");

            try
            {
                // Simulate an operation that might throw an exception
                int result = 10 / 1; // This won't throw, but let's log it
                logger.Info($"Division result: {result}");
            }
            catch (Exception ex)
            {
                logger.Error("An error occurred during division", ex);
            }

            Console.WriteLine($"Log file location: {logger.GetLogFilePath()}");
            logger.Info($"Displayed log file location to user: {logger.GetLogFilePath()}");
            logger.Info("Application ending normally");

            Console.WriteLine("Press any  key to continue...");
            logger.Debug("Prompted user to press any key to continue");

            // Check if we can read keys (handles both debugger and console scenarios)
            try
            {
                if (Console.IsInputRedirected)
                {
                    Console.WriteLine("(Input redirected - continuing automatically)");
                    logger.Info("Input was redirected - continued automatically");
                }
                else
                {
                    Console.ReadKey(true);
                }
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("(Console input not available - continuing automatically)");
                logger.Warning("Console input not available - continued automatically");
            }

            // Clean up logging
            ConsoleLogger.DisableConsoleLogging();
        }
    }
}
