using System.Text.RegularExpressions;
using FileConventionValidator.Models;
using Microsoft.Extensions.Logging;

namespace FileConventionValidator.Services
{
    /// <summary>
    /// Service for validating file names against naming conventions
    /// </summary>
    public interface IFileConventionValidationService
    {
        FileValidationDetails ValidateFileName(string fileName, FileConventionSettings settings);
        bool IsValidExtension(string fileName, FileConventionSettings settings);
        List<string> ValidateFileNameConstraints(string fileName, ValidationSettings settings);
    }

    /// <summary>
    /// Implementation of file convention validation service
    /// </summary>
    public class FileConventionValidationService : IFileConventionValidationService
    {
        private readonly ILogger<FileConventionValidationService> _logger;
        private readonly Dictionary<string, Regex> _compiledPatterns;

        public FileConventionValidationService(ILogger<FileConventionValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _compiledPatterns = new Dictionary<string, Regex>();
        }

        /// <summary>
        /// Validates a file name against all configured naming conventions
        /// </summary>
        public FileValidationDetails ValidateFileName(string fileName, FileConventionSettings settings)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Attempted to validate null or empty file name");
                return new FileValidationDetails
                {
                    FileName = fileName ?? string.Empty,
                    IsValid = false,
                    ValidationErrors = new List<string> { "File name cannot be null or empty" }
                };
            }

            var result = new FileValidationDetails
            {
                FileName = fileName,
                FilePath = string.Empty // Will be set by caller
            };

            _logger.LogDebug("Validating file name: {FileName}", fileName);

            try
            {
                // Check basic file name constraints
                var constraintErrors = ValidateFileNameConstraints(fileName, settings.ValidationSettings);
                result.ValidationErrors.AddRange(constraintErrors);

                // Check file extension
                if (!IsValidExtension(fileName, settings))
                {
                    result.ValidationErrors.Add($"File extension not in allowed list: {string.Join(", ", settings.FileExtensions)}");
                }

                // Check against naming conventions
                var conventionMatch = ValidateAgainstConventions(fileName, settings);
                if (conventionMatch.IsValid)
                {
                    result.MatchedPattern = conventionMatch.Pattern;
                    result.MatchedConventionName = conventionMatch.ConventionName;
                    _logger.LogDebug("File '{FileName}' matched convention '{ConventionName}'", fileName, conventionMatch.ConventionName);
                }
                else
                {
                    result.ValidationErrors.Add("File name does not match any configured naming convention");
                    if (settings.NamingConventions.Any())
                    {
                        result.ValidationErrors.Add($"Expected patterns: {string.Join(", ", settings.NamingConventions.Select(c => c.Name))}");
                    }
                }

                result.IsValid = !result.ValidationErrors.Any();

                if (result.IsValid)
                {
                    _logger.LogInformation("File '{FileName}' passed validation", fileName);
                }
                else
                {
                    _logger.LogWarning("File '{FileName}' failed validation: {Errors}", fileName, string.Join("; ", result.ValidationErrors));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file name '{FileName}'", fileName);
                result.IsValid = false;
                result.ValidationErrors.Add($"Validation error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Checks if file extension is in the allowed list
        /// </summary>
        public bool IsValidExtension(string fileName, FileConventionSettings settings)
        {
            if (!settings.FileExtensions.Any())
            {
                return true; // No restrictions
            }

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            var comparison = settings.ValidationSettings.CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            return settings.FileExtensions.Any(ext =>
                string.Equals(extension, ext.StartsWith(".") ? ext : "." + ext, comparison));
        }

        /// <summary>
        /// Validates basic file name constraints (length, characters, etc.)
        /// </summary>
        public List<string> ValidateFileNameConstraints(string fileName, ValidationSettings settings)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(fileName))
            {
                errors.Add("File name cannot be null or empty");
                return errors;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            // Length validation
            if (fileName.Length > settings.MaxFileNameLength)
            {
                errors.Add($"File name exceeds maximum length of {settings.MaxFileNameLength} characters");
            }

            if (nameWithoutExtension.Length < settings.MinFileNameLength)
            {
                errors.Add($"File name is shorter than minimum length of {settings.MinFileNameLength} characters");
            }

            // Space validation
            if (!settings.AllowSpaces && fileName.Contains(' '))
            {
                errors.Add("File name contains spaces which are not allowed");
            }

            // Invalid characters check
            var invalidChars = Path.GetInvalidFileNameChars();
            var hasInvalidChars = fileName.Any(c => invalidChars.Contains(c));
            if (hasInvalidChars)
            {
                errors.Add("File name contains invalid characters");
            }

            // Reserved names check (Windows)
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            if (reservedNames.Contains(nameWithoutExtension.ToUpperInvariant()))
            {
                errors.Add($"File name '{nameWithoutExtension}' is a reserved system name");
            }

            return errors;
        }

        /// <summary>
        /// Validates file name against all configured naming conventions
        /// </summary>
        private (bool IsValid, string? Pattern, string? ConventionName) ValidateAgainstConventions(string fileName, FileConventionSettings settings)
        {
            if (!settings.NamingConventions.Any())
            {
                _logger.LogWarning("No naming conventions configured - all files will be considered invalid");
                return (false, null, null);
            }

            foreach (var convention in settings.NamingConventions)
            {
                try
                {
                    var regex = GetCompiledRegex(convention.Pattern, settings.ValidationSettings.CaseSensitive);
                    var nameToTest = Path.GetFileNameWithoutExtension(fileName);

                    if (regex.IsMatch(nameToTest))
                    {
                        _logger.LogDebug("File '{FileName}' matches convention '{ConventionName}' with pattern '{Pattern}'",
                            fileName, convention.Name, convention.Pattern);
                        return (true, convention.Pattern, convention.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error testing convention '{ConventionName}' with pattern '{Pattern}' against file '{FileName}'",
                        convention.Name, convention.Pattern, fileName);
                }
            }

            return (false, null, null);
        }

        /// <summary>
        /// Gets a compiled regex pattern, caching for performance
        /// </summary>
        private Regex GetCompiledRegex(string pattern, bool caseSensitive)
        {
            var cacheKey = $"{pattern}_{caseSensitive}";

            if (_compiledPatterns.TryGetValue(cacheKey, out var cachedRegex))
            {
                return cachedRegex;
            }

            var options = RegexOptions.Compiled;
            if (!caseSensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }

            try
            {
                var regex = new Regex(pattern, options, TimeSpan.FromSeconds(1)); // Timeout for safety
                _compiledPatterns[cacheKey] = regex;
                _logger.LogDebug("Compiled and cached regex pattern: {Pattern}", pattern);
                return regex;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile regex pattern: {Pattern}", pattern);
                throw new ArgumentException($"Invalid regex pattern: {pattern}", ex);
            }
        }
    }
}