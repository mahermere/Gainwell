namespace FileProcessor.Models
{
    /// <summary>
    /// Configuration settings for file processing
    /// </summary>
    public class FileProcessingSettings
    {
        public string Delimiter { get; set; } = "|";
        public int FileNameFieldIndex { get; set; } = 2;
        public string RootPath { get; set; } = string.Empty;
        public string ZeroBytePath { get; set; } = string.Empty;
        public bool CreateDirectories { get; set; } = true;
        public string LogLevel { get; set; } = "Information";
    }

    /// <summary>
    /// Result of file processing operation
    /// </summary>
    public class ProcessingResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalLinesProcessed { get; set; }
        public int ValidLinesWritten { get; set; }
        public int ZeroByteFilesMoved { get; set; }
        public int MissingFiles { get; set; }
        public int ErrorLines { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Information about a processed line
    /// </summary>
    public class LineProcessingInfo
    {
        public int LineNumber { get; set; }
        public string OriginalLine { get; set; } = string.Empty;
        public string[] Fields { get; set; } = Array.Empty<string>();
        public string? FileName { get; set; }
        public string? FullFilePath { get; set; }
        public bool FileExists { get; set; }
        public long FileSize { get; set; }
        public ProcessingAction Action { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Action taken for a processed line
    /// </summary>
    public enum ProcessingAction
    {
        Unknown,
        WrittenToOutput,
        FileMovedToZeroBytes,
        FileNotFound,
        InvalidLine,
        Error
    }
}