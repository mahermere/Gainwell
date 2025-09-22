namespace FileConventionValidator.Models
{
    /// <summary>
    /// Configuration settings for file convention validation
    /// </summary>
    public class FileConventionSettings
    {
        public string InputDirectory { get; set; } = string.Empty;
        public string ReadyDirectory { get; set; } = string.Empty;
        public string ReviewDirectory { get; set; } = string.Empty;
        public bool CreateDirectories { get; set; } = true;
        public bool ProcessSubdirectories { get; set; } = false;
        public List<string> FileExtensions { get; set; } = new();
        public List<NamingConvention> NamingConventions { get; set; } = new();
        public ValidationSettings ValidationSettings { get; set; } = new();
    }

    /// <summary>
    /// Naming convention definition with pattern and metadata
    /// </summary>
    public class NamingConvention
    {
        public string Name { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Examples { get; set; } = new();
    }

    /// <summary>
    /// Additional validation settings
    /// </summary>
    public class ValidationSettings
    {
        public bool CaseSensitive { get; set; } = false;
        public bool AllowSpaces { get; set; } = false;
        public int MaxFileNameLength { get; set; } = 255;
        public int MinFileNameLength { get; set; } = 3;
    }

    /// <summary>
    /// Result of file validation and processing
    /// </summary>
    public class ValidationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalFilesProcessed { get; set; }
        public int FilesMovedToReady { get; set; }
        public int FilesMovedToReview { get; set; }
        public int ErrorsEncountered { get; set; }
        public List<FileProcessingInfo> ProcessedFiles { get; set; } = new();
    }

    /// <summary>
    /// Information about a processed file
    /// </summary>
    public class FileProcessingInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public string? DestinationPath { get; set; }
        public bool IsValid { get; set; }
        public string? MatchedConvention { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public FileAction Action { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Action taken for a processed file
    /// </summary>
    public enum FileAction
    {
        None,
        MovedToReady,
        MovedToReview,
        Skipped,
        Error
    }

    /// <summary>
    /// File validation details
    /// </summary>
    public class FileValidationDetails
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? MatchedPattern { get; set; }
        public string? MatchedConventionName { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
    }
}