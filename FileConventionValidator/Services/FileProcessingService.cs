using FileConventionValidator.Models;
using Microsoft.Extensions.Logging;

namespace FileConventionValidator.Services
{
    /// <summary>
    /// Service for processing files and moving them based on validation results
    /// </summary>
    public interface IFileProcessingService
    {
        Task<ValidationResult> ProcessDirectoryAsync(FileConventionSettings settings);
        Task<FileProcessingInfo> ProcessFileAsync(string filePath, FileConventionSettings settings);
        Task<bool> EnsureDirectoriesExistAsync(FileConventionSettings settings);
    }

    /// <summary>
    /// Implementation of file processing service
    /// </summary>
    public class FileProcessingService : IFileProcessingService
    {
        private readonly IFileConventionValidationService _validationService;
        private readonly ILogger<FileProcessingService> _logger;

        public FileProcessingService(
            IFileConventionValidationService validationService,
            ILogger<FileProcessingService> logger)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes all files in the input directory
        /// </summary>
        public async Task<ValidationResult> ProcessDirectoryAsync(FileConventionSettings settings)
        {
            var result = new ValidationResult { Success = false };

            try
            {
                _logger.LogInformation("Starting directory processing for: {InputDirectory}", settings.InputDirectory);

                // Validate settings
                if (!ValidateSettings(settings, result))
                {
                    return result;
                }

                // Ensure directories exist
                if (!await EnsureDirectoriesExistAsync(settings))
                {
                    result.ErrorMessage = "Failed to create required directories";
                    return result;
                }

                // Get files to process
                var filesToProcess = GetFilesToProcess(settings);
                result.TotalFilesProcessed = filesToProcess.Count;

                _logger.LogInformation("Found {FileCount} files to process", filesToProcess.Count);

                if (!filesToProcess.Any())
                {
                    _logger.LogInformation("No files found to process");
                    result.Success = true;
                    return result;
                }

                // Process each file
                foreach (var filePath in filesToProcess)
                {
                    try
                    {
                        var fileResult = await ProcessFileAsync(filePath, settings);
                        result.ProcessedFiles.Add(fileResult);

                        switch (fileResult.Action)
                        {
                            case FileAction.MovedToReady:
                                result.FilesMovedToReady++;
                                break;
                            case FileAction.MovedToReview:
                                result.FilesMovedToReview++;
                                break;
                            case FileAction.Error:
                                result.ErrorsEncountered++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
                        result.ErrorsEncountered++;
                        result.ProcessedFiles.Add(new FileProcessingInfo
                        {
                            FileName = Path.GetFileName(filePath),
                            OriginalPath = filePath,
                            IsValid = false,
                            ValidationErrors = new List<string> { $"Processing error: {ex.Message}" },
                            Action = FileAction.Error
                        });
                    }
                }

                result.Success = result.ErrorsEncountered == 0 || result.ErrorsEncountered < result.TotalFilesProcessed;

                _logger.LogInformation("Directory processing completed. Success: {Success}, Total: {Total}, Ready: {Ready}, Review: {Review}, Errors: {Errors}",
                    result.Success, result.TotalFilesProcessed, result.FilesMovedToReady, result.FilesMovedToReview, result.ErrorsEncountered);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during directory processing");
                result.ErrorMessage = $"Directory processing failed: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Processes a single file
        /// </summary>
        public async Task<FileProcessingInfo> ProcessFileAsync(string filePath, FileConventionSettings settings)
        {
            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileProcessingInfo
            {
                FileName = fileName,
                OriginalPath = filePath
            };

            try
            {
                _logger.LogDebug("Processing file: {FileName}", fileName);

                // Validate the file name
                var validationDetails = _validationService.ValidateFileName(fileName, settings);
                fileInfo.IsValid = validationDetails.IsValid;
                fileInfo.MatchedConvention = validationDetails.MatchedConventionName;
                fileInfo.ValidationErrors = validationDetails.ValidationErrors;

                // Determine destination and move file
                if (validationDetails.IsValid)
                {
                    // Move to Ready folder
                    var destinationPath = Path.Combine(settings.ReadyDirectory, fileName);
                    if (await MoveFileAsync(filePath, destinationPath))
                    {
                        fileInfo.DestinationPath = destinationPath;
                        fileInfo.Action = FileAction.MovedToReady;
                        _logger.LogInformation("File '{FileName}' moved to Ready folder", fileName);
                    }
                    else
                    {
                        fileInfo.Action = FileAction.Error;
                        fileInfo.ValidationErrors.Add("Failed to move file to Ready folder");
                    }
                }
                else
                {
                    // Move to Review folder
                    var destinationPath = Path.Combine(settings.ReviewDirectory, fileName);
                    if (await MoveFileAsync(filePath, destinationPath))
                    {
                        fileInfo.DestinationPath = destinationPath;
                        fileInfo.Action = FileAction.MovedToReview;
                        _logger.LogInformation("File '{FileName}' moved to Review folder due to validation errors", fileName);
                    }
                    else
                    {
                        fileInfo.Action = FileAction.Error;
                        fileInfo.ValidationErrors.Add("Failed to move file to Review folder");
                    }
                }

                return fileInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
                fileInfo.Action = FileAction.Error;
                fileInfo.ValidationErrors.Add($"Processing error: {ex.Message}");
                return fileInfo;
            }
        }

        /// <summary>
        /// Ensures required directories exist
        /// </summary>
        public async Task<bool> EnsureDirectoriesExistAsync(FileConventionSettings settings)
        {
            try
            {
                if (!settings.CreateDirectories)
                {
                    return Directory.Exists(settings.InputDirectory) &&
                           Directory.Exists(settings.ReadyDirectory) &&
                           Directory.Exists(settings.ReviewDirectory);
                }

                var directories = new[] { settings.InputDirectory, settings.ReadyDirectory, settings.ReviewDirectory };

                foreach (var directory in directories)
                {
                    if (!Directory.Exists(directory))
                    {
                        _logger.LogInformation("Creating directory: {Directory}", directory);
                        Directory.CreateDirectory(directory);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directories");
                return false;
            }
        }

        /// <summary>
        /// Gets list of files to process from input directory
        /// </summary>
        private List<string> GetFilesToProcess(FileConventionSettings settings)
        {
            try
            {
                if (!Directory.Exists(settings.InputDirectory))
                {
                    _logger.LogWarning("Input directory does not exist: {InputDirectory}", settings.InputDirectory);
                    return new List<string>();
                }

                var searchOption = settings.ProcessSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var allFiles = Directory.GetFiles(settings.InputDirectory, "*", searchOption);

                // Filter by extensions if specified
                if (settings.FileExtensions.Any())
                {
                    var allowedExtensions = settings.FileExtensions
                        .Select(ext => ext.StartsWith(".") ? ext : "." + ext)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    allFiles = allFiles
                        .Where(file => allowedExtensions.Contains(Path.GetExtension(file)))
                        .ToArray();
                }

                _logger.LogDebug("Found {FileCount} files matching criteria", allFiles.Length);
                return allFiles.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files from input directory: {InputDirectory}", settings.InputDirectory);
                return new List<string>();
            }
        }

        /// <summary>
        /// Moves a file from source to destination
        /// </summary>
        private async Task<bool> MoveFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                // Handle file name conflicts
                if (File.Exists(destinationPath))
                {
                    var directory = Path.GetDirectoryName(destinationPath) ?? string.Empty;
                    var fileName = Path.GetFileNameWithoutExtension(destinationPath);
                    var extension = Path.GetExtension(destinationPath);
                    var counter = 1;

                    do
                    {
                        destinationPath = Path.Combine(directory, $"{fileName}_{counter:D3}{extension}");
                        counter++;
                    } while (File.Exists(destinationPath) && counter < 1000);

                    if (counter >= 1000)
                    {
                        _logger.LogError("Unable to find unique file name for: {OriginalPath}", sourcePath);
                        return false;
                    }

                    _logger.LogDebug("File name conflict resolved, using: {NewPath}", destinationPath);
                }

                // Ensure destination directory exists
                var destDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                // Move the file
                File.Move(sourcePath, destinationPath);
                _logger.LogDebug("File moved from '{Source}' to '{Destination}'", sourcePath, destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file from '{Source}' to '{Destination}'", sourcePath, destinationPath);
                return false;
            }
        }

        /// <summary>
        /// Validates the settings configuration
        /// </summary>
        private bool ValidateSettings(FileConventionSettings settings, ValidationResult result)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(settings.InputDirectory))
                errors.Add("Input directory is required");

            if (string.IsNullOrWhiteSpace(settings.ReadyDirectory))
                errors.Add("Ready directory is required");

            if (string.IsNullOrWhiteSpace(settings.ReviewDirectory))
                errors.Add("Review directory is required");

            if (!settings.NamingConventions.Any())
                errors.Add("At least one naming convention must be configured");

            if (errors.Any())
            {
                result.ErrorMessage = string.Join("; ", errors);
                _logger.LogError("Settings validation failed: {Errors}", result.ErrorMessage);
                return false;
            }

            return true;
        }
    }
}