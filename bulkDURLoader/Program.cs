using System;
using Microsoft.Extensions.Configuration;
using bulkDURLoader.Logging;
using bulkDURLoader.Database;
using bulkDURLoader.Utilities;
using bulkDURLoader.BulkLoading;
using bulkDURLoader.Models;
using bulkDURLoader.DataLoading;

namespace bulkDURLoader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize logging system - this will capture all Console.WriteLine calls
            ConsoleLogger.EnableConsoleLogging();
            var logger = Logger.Instance;

            logger.Info("bulkDURLoader application started");

            // Log runtime and assembly information
            RuntimeInfo.LogRuntimeInformation();
            RuntimeInfo.LogAssemblyInformation();

            // Initialize configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            logger.Info("Configuration loaded successfully");

            Console.WriteLine("Hello, bulkDURLoader Application!");
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
                // Initialize database configuration
                var dbConfig = new DatabaseConfig(configuration);
                logger.Info("Database configuration initialized");

                // Log database connection information (without sensitive data)
                dbConfig.LogConnectionInfo();

                // Test Oracle database connection
                logger.Info("Testing Oracle database connection...");
                bool connectionSuccessful = await dbConfig.TestConnectionAsync();

                if (connectionSuccessful)
                {
                    logger.Info("Oracle database connection established successfully");
                    Console.WriteLine("✓ Oracle database connection successful!");

                    // Example: Execute a simple query
                    using var connection = await dbConfig.GetConnectionAsync();
                    using var command = new Oracle.ManagedDataAccess.Client.OracleCommand("SELECT SYSDATE FROM DUAL", connection);
                    var currentDate = await command.ExecuteScalarAsync();

                    logger.Info($"Database current date/time: {currentDate}");
                    Console.WriteLine($"Database server time: {currentDate}");

                    // Demonstrate Oracle bulk loading functionality
                    await DemonstrateBulkLoadingAsync(dbConfig, configuration, logger);
                }
                else
                {
                    logger.Error("Failed to establish Oracle database connection");
                    Console.WriteLine("✗ Oracle database connection failed!");
                }
            }
            catch (Exception ex)
            {
                logger.Error("An error occurred during database operations", ex);
                Console.WriteLine($"✗ Database operation failed: {ex.Message}");
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

        /// <summary>
        /// Demonstrates Oracle bulk loading functionality with CSV data or sample data
        /// </summary>
        private static async Task DemonstrateBulkLoadingAsync(DatabaseConfig dbConfig, IConfiguration configuration, Logger logger)
        {
            try
            {
                logger.Info("Starting Oracle bulk loading demonstration");
                Console.WriteLine("\n--- Oracle Bulk Loading Demonstration ---");

                // Initialize the bulk loader
                var bulkLoader = new OracleBulkLoader(dbConfig, configuration);

                // Check if CSV file should be used
                var csvFilePath = configuration.GetValue<string>("CsvSettings:InputFilePath");
                List<DurQuarterlyLoad> dataToLoad;

                if (!string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath))
                {
                    // Load data from CSV file
                    dataToLoad = await LoadDataFromCsvAsync(csvFilePath, configuration, logger);
                }
                else
                {
                    // Generate sample data
                    dataToLoad = await GenerateSampleDataAsync(configuration, logger);
                }

                if (dataToLoad == null || !dataToLoad.Any())
                {
                    logger.Warning("No data available for bulk loading");
                    Console.WriteLine("⚠ No data available for bulk loading");
                    return;
                }

                // Perform bulk insert
                logger.Info($"Starting bulk insert operation with {dataToLoad.Count} records");
                Console.WriteLine($"Starting bulk insert to ccBulk_DUR_QTR_LOAD table with {dataToLoad.Count} records...");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var insertedCount = await bulkLoader.BulkInsertAsync(dataToLoad);
                stopwatch.Stop();

                logger.Info($"Bulk insert completed. Records inserted: {insertedCount}, Time elapsed: {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"✓ Bulk insert completed successfully!");
                Console.WriteLine($"  Records inserted: {insertedCount}");
                Console.WriteLine($"  Time elapsed: {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"  Performance: {(double)insertedCount / stopwatch.ElapsedMilliseconds * 1000:F1} records/second");

                // Verify the insert by counting records
                if (dataToLoad.Any())
                {
                    var batchId = dataToLoad.First().BatchId;
                    if (!string.IsNullOrEmpty(batchId))
                    {
                        await VerifyBulkInsertAsync(dbConfig, batchId, logger);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Bulk loading demonstration failed", ex);
                Console.WriteLine($"✗ Bulk loading failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Load data from CSV file
        /// </summary>
        private static async Task<List<DurQuarterlyLoad>> LoadDataFromCsvAsync(string csvFilePath, IConfiguration configuration, Logger logger)
        {
            try
            {
                logger.Info($"Loading data from CSV file: {csvFilePath}");
                Console.WriteLine($"Loading data from CSV file: {csvFilePath}");

                var csvReader = new DurCsvReader(configuration);

                // Get file information
                var fileInfo = await csvReader.GetFileInfoAsync(csvFilePath);
                logger.Info($"CSV file info - Size: {fileInfo.FileSize} bytes, Estimated records: {fileInfo.EstimatedRecords}");
                Console.WriteLine($"CSV file contains approximately {fileInfo.EstimatedRecords} records ({fileInfo.FileSize} bytes)");
                Console.WriteLine($"Headers found: {string.Join(", ", fileInfo.Headers)}");

                // Read the CSV data
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var records = await csvReader.ReadCsvFileAsync(csvFilePath);
                stopwatch.Stop();

                logger.Info($"CSV file loaded. Records: {records.Count}, Time: {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"✓ CSV file loaded: {records.Count} records in {stopwatch.ElapsedMilliseconds}ms");

                // Set batch ID for all records if not already set
                var batchId = $"CSV_{DateTime.Now:yyyyMMdd_HHmmss}";
                foreach (var record in records.Where(r => string.IsNullOrEmpty(r.BatchId)))
                {
                    record.BatchId = batchId;
                }

                return records;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load data from CSV file: {csvFilePath}", ex);
                Console.WriteLine($"✗ Failed to load CSV file: {ex.Message}");
                return new List<DurQuarterlyLoad>();
            }
        }

        /// <summary>
        /// Generate sample data for testing
        /// </summary>
        private static async Task<List<DurQuarterlyLoad>> GenerateSampleDataAsync(IConfiguration configuration, Logger logger)
        {
            await Task.Delay(1); // Make async to match signature

            var currentQuarter = $"Q{(DateTime.Now.Month - 1) / 3 + 1}";
            var currentYear = DateTime.Now.Year;
            var batchId = $"SAMPLE_{DateTime.Now:yyyyMMdd_HHmmss}";
            var recordCount = configuration.GetValue<int>("BulkLoadSettings:SampleDataCount", 100);

            logger.Info($"Generating {recordCount} sample DUR records for {currentQuarter} {currentYear}");
            Console.WriteLine($"No CSV file found, generating {recordCount} sample DUR records for {currentQuarter} {currentYear}...");

            var sampleData = OracleBulkLoader.GenerateSampleData(recordCount, currentQuarter, currentYear, batchId);

            logger.Info($"Generated {sampleData.Count} sample records with batch ID: {batchId}");
            Console.WriteLine($"Generated {sampleData.Count} sample records");

            return sampleData;
        }

        /// <summary>
        /// Verifies the bulk insert by counting records in the database
        /// </summary>
        private static async Task VerifyBulkInsertAsync(DatabaseConfig dbConfig, string batchId, Logger logger)
        {
            try
            {
                logger.Info($"Verifying bulk insert for batch ID: {batchId}");

                using var connection = await dbConfig.GetConnectionAsync();
                using var command = new Oracle.ManagedDataAccess.Client.OracleCommand(
                    "SELECT COUNT(*) FROM ccBulk_DUR_QTR_LOAD WHERE BATCH_ID = :BatchId", connection);
                command.Parameters.Add("BatchId", batchId);

                var count = await command.ExecuteScalarAsync();
                logger.Info($"Verification complete. Records found in database: {count}");
                Console.WriteLine($"✓ Verification: {count} records found in database for batch {batchId}");
            }
            catch (Exception ex)
            {
                logger.Warning($"Verification failed: {ex.Message}");
                Console.WriteLine($"⚠ Verification failed: {ex.Message}");
            }
        }
    }
}
