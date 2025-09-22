using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FileProcessor.Models;

namespace FileProcessor.Services
{
    /// <summary>
    /// Interface for file processing service
    /// </summary>
    public interface IFileProcessingService
    {
        Task<ProcessingResult> ProcessFileAsync(string inputPath, string outputPath);
    }

    /// <summary>
    /// Main file processing service that handles line-by-line processing
    /// </summary>
    public class FileProcessingService : IFileProcessingService
    {
        private readonly ILogger<FileProcessingService> _logger;
        private readonly FileProcessingSettings _settings;

        public FileProcessingService(ILogger<FileProcessingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _settings = new FileProcessingSettings();
            configuration.GetSection("FileProcessingSettings").Bind(_settings);

            ValidateSettings();
        }

        public async Task<ProcessingResult> ProcessFileAsync(string inputPath, string outputPath)
        {
            var result = new ProcessingResult();

            try
            {
                _logger.LogInformation("Starting file processing - Input: {InputPath}, Output: {OutputPath}", inputPath, outputPath);

                // Validate input file exists
                if (!File.Exists(inputPath))
                {
                    result.ErrorMessage = $"Input file not found: {inputPath}";
                    _logger.LogError(result.ErrorMessage);
                    return result;
                }

                // Create output directory if needed
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    _logger.LogInformation("Created output directory: {OutputDir}", outputDir);
                }

                // Create zero byte directory if needed
                if (_settings.CreateDirectories && !Directory.Exists(_settings.ZeroBytePath))
                {
                    Directory.CreateDirectory(_settings.ZeroBytePath);
                    _logger.LogInformation("Created zero byte directory: {ZeroBytePath}", _settings.ZeroBytePath);
                }

                // Process the file line by line
                using var inputReader = new StreamReader(inputPath);
                using var outputWriter = new StreamWriter(outputPath, append: false);

                string? line;
                int lineNumber = 0;

                while ((line = await inputReader.ReadLineAsync()) != null)
                {
                    lineNumber++;
                    result.TotalLinesProcessed++;

                    var lineInfo = ProcessLine(line, lineNumber);

                    switch (lineInfo.Action)
                    {
                        case ProcessingAction.WrittenToOutput:
                            await outputWriter.WriteLineAsync(lineInfo.OriginalLine);
                            result.ValidLinesWritten++;
                            _logger.LogDebug("Line {LineNumber}: Written to output - File: {FileName}",
                                lineNumber, lineInfo.FileName);
                            break;

                        case ProcessingAction.FileMovedToZeroBytes:
                            result.ZeroByteFilesMoved++;
                            _logger.LogInformation("Line {LineNumber}: Moved zero-byte file - {FileName}",
                                lineNumber, lineInfo.FileName);
                            break;

                        case ProcessingAction.FileNotFound:
                            result.MissingFiles++;
                            _logger.LogWarning("Line {LineNumber}: File not found - {FileName}",
                                lineNumber, lineInfo.FileName);
                            break;

                        case ProcessingAction.InvalidLine:
                        case ProcessingAction.Error:
                            result.ErrorLines++;
                            result.Errors.Add($"Line {lineNumber}: {lineInfo.ErrorMessage}");
                            _logger.LogError("Line {LineNumber}: {ErrorMessage}", lineNumber, lineInfo.ErrorMessage);
                            break;
                    }

                    // Log progress every 1000 lines
                    if (lineNumber % 1000 == 0)
                    {
                        _logger.LogInformation("Processed {LineNumber} lines...", lineNumber);
                    }
                }

                result.Success = true;
                _logger.LogInformation("File processing completed successfully");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Processing failed: {ex.Message}";
                _logger.LogError(ex, "File processing failed");
            }

            return result;
        }

        private LineProcessingInfo ProcessLine(string line, int lineNumber)
        {
            var info = new LineProcessingInfo
            {
                LineNumber = lineNumber,
                OriginalLine = line
            };

            try
            {
                // Split the line by delimiter
                info.Fields = line.Split(_settings.Delimiter, StringSplitOptions.None);

                // Check if we have enough fields
                if (info.Fields.Length <= _settings.FileNameFieldIndex)
                {
                    info.Action = ProcessingAction.InvalidLine;
                    info.ErrorMessage = $"Line has {info.Fields.Length} fields, but field index {_settings.FileNameFieldIndex} is required";
                    return info;
                }

                // Get the filename from the specified field
                info.FileName = info.Fields[_settings.FileNameFieldIndex].Trim();

                if (string.IsNullOrEmpty(info.FileName))
                {
                    info.Action = ProcessingAction.InvalidLine;
                    info.ErrorMessage = "Filename field is empty";
                    return info;
                }

                // Construct full file path
                info.FullFilePath = Path.Combine(_settings.RootPath, info.FileName);

                // Check if file exists
                if (!File.Exists(info.FullFilePath))
                {
                    info.Action = ProcessingAction.FileNotFound;
                    info.ErrorMessage = $"File not found: {info.FullFilePath}";
                    return info;
                }

                info.FileExists = true;

                // Get file size
                var fileInfo = new FileInfo(info.FullFilePath);
                info.FileSize = fileInfo.Length;

                // Handle based on file size
                if (info.FileSize == 0)
                {
                    // Move zero-byte file
                    MoveZeroByteFile(info);
                    info.Action = ProcessingAction.FileMovedToZeroBytes;
                }
                else
                {
                    // File has content, write line to output
                    info.Action = ProcessingAction.WrittenToOutput;
                }
            }
            catch (Exception ex)
            {
                info.Action = ProcessingAction.Error;
                info.ErrorMessage = $"Error processing line: {ex.Message}";
            }

            return info;
        }

        private void MoveZeroByteFile(LineProcessingInfo info)
        {
            try
            {
                if (string.IsNullOrEmpty(info.FullFilePath) || string.IsNullOrEmpty(info.FileName))
                    return;

                var destinationPath = Path.Combine(_settings.ZeroBytePath, info.FileName);

                // Handle duplicate names by adding a timestamp
                if (File.Exists(destinationPath))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(info.FileName);
                    var extension = Path.GetExtension(info.FileName);
                    var newFileName = $"{nameWithoutExt}_{timestamp}{extension}";
                    destinationPath = Path.Combine(_settings.ZeroBytePath, newFileName);
                }

                // Move the file
                File.Move(info.FullFilePath, destinationPath);

                _logger.LogDebug("Moved zero-byte file from {Source} to {Destination}",
                    info.FullFilePath, destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move zero-byte file: {FilePath}", info.FullFilePath);
                throw;
            }
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrEmpty(_settings.RootPath))
            {
                throw new InvalidOperationException("RootPath is required in configuration");
            }

            if (string.IsNullOrEmpty(_settings.ZeroBytePath))
            {
                throw new InvalidOperationException("ZeroBytePath is required in configuration");
            }

            if (_settings.FileNameFieldIndex < 0)
            {
                throw new InvalidOperationException("FileNameFieldIndex must be >= 0");
            }

            if (string.IsNullOrEmpty(_settings.Delimiter))
            {
                throw new InvalidOperationException("Delimiter is required in configuration");
            }

            _logger.LogInformation("File processing settings validated:");
            _logger.LogInformation("  Delimiter: '{Delimiter}'", _settings.Delimiter);
            _logger.LogInformation("  Filename field index: {Index}", _settings.FileNameFieldIndex);
            _logger.LogInformation("  Root path: {RootPath}", _settings.RootPath);
            _logger.LogInformation("  Zero byte path: {ZeroBytePath}", _settings.ZeroBytePath);
        }
    }
}